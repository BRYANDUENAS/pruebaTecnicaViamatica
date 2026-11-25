using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Viamatica.Application.Abstractions;
using Viamatica.Application.Common;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Infrastructure.Persistence;

/// <summary>
/// Datos de demostración: gestores, cajeros, cajas, clientes, contratos, pagos y
/// turnos. Se ejecuta una sola vez, cuando la base no tiene clientes, y solo si
/// <c>Seed:DemoData</c> está activo. Los datos son ficticios.
/// </summary>
public static class DemoSeeder
{
    public const string GestorPassword = "Gestor1234";
    public const string CajeroPassword = "Cajero1234";

    private static readonly string[] FirstNames =
    [
        "María", "José", "Luis", "Ana", "Carlos", "Gabriela", "Jorge", "Andrea", "Diego", "Paola",
        "Fernando", "Daniela", "Ricardo", "Verónica", "Miguel", "Carolina", "Andrés", "Lorena", "Pablo", "Mónica",
    ];

    private static readonly string[] LastNames =
    [
        "Zambrano", "Cedeño", "Mendoza", "Vera", "Macías", "Moreira", "Alvarado", "Torres", "Quiñónez", "Loor",
        "Intriago", "Bravo", "Chávez", "Salazar", "Espinoza", "Villamar", "Franco", "Suárez", "Ortiz", "Castro",
    ];

    private static readonly string[] Streets =
    [
        "Av. Francisco de Orellana y Justino Cornejo", "Cdla. Kennedy Norte, calle Miguel H. Alcívar",
        "Av. 9 de Octubre y Boyacá, edificio Central", "Urdesa Central, Víctor Emilio Estrada 512",
        "Cdla. Alborada 6ta etapa, manzana 624 villa 8", "Samanes 2, manzana 215 villa 14, vía principal",
        "Av. Juan Tanca Marengo km 3.5, conjunto Los Olivos", "Sauces 8, manzana 451 villa 22, calle peatonal",
        "Los Ceibos, calle Octava 210 y avenida Segunda", "Cdla. La Garzota, manzana 12 villa 5, calle 3ra",
    ];

    private static readonly string[] References =
    [
        "Frente al parque principal de la ciudadela", "Diagonal a la farmacia de la esquina",
        "A una cuadra del centro comercial del sector", "Junto a la gasolinera, casa de dos pisos blanca",
        "Detrás de la iglesia, portón de color negro", "Al lado de la unidad educativa del barrio",
    ];

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Clients.IgnoreQueryFilters().AnyAsync(ct)) return;

        var random = new Random(2026);
        var now = DateTime.UtcNow;
        var admin = await db.Users.FirstAsync(u => u.RolId == RoleCodes.AdminId, ct);

        // ----- Usuarios: 3 gestores y 13 cajeros en distintos estados -----
        var gestorHash = hasher.Hash(GestorPassword);
        var cajeroHash = hasher.Hash(CajeroPassword);
        var sequence = 0;

        User NewUser(string username, string hash, int rolId, string statusId, int creatorId, bool session = false)
        {
            sequence++;
            var first = FirstNames[(sequence * 3) % FirstNames.Length];
            var last = LastNames[(sequence * 7) % LastNames.Length];
            var approved = statusId != UserStatusCodes.Pending;
            var created = now.AddDays(-random.Next(20, 200));
            return new User
            {
                Username = username,
                Password = hash,
                FirstName = first,
                LastName = last,
                Email = $"{username}@netcaja.demo",
                Identification = $"09{10000000 + sequence * 137911 % 89999999:D8}",
                RolId = rolId,
                StatusId = statusId,
                CreationDate = created,
                UserCreate = creatorId,
                UserApproval = approved ? admin.UserId : null,
                DateApproval = approved ? created.AddHours(3) : null,
                SessionActive = session,
                LastLogin = session ? now.AddMinutes(-random.Next(10, 180)) : now.AddDays(-random.Next(1, 9)),
                FailedAttempts = statusId == UserStatusCodes.Blocked ? 3 : 0,
            };
        }

        var gestores = Enumerable.Range(1, 3)
            .Select(i => NewUser($"gestor{i:D3}", gestorHash, RoleCodes.GestorId, UserStatusCodes.Active, admin.UserId, session: i == 1))
            .ToList();
        db.Users.AddRange(gestores);
        await db.SaveChangesAsync(ct);

        var cajeros = Enumerable.Range(1, 10)
            .Select(i => NewUser($"cajero{i:D3}", cajeroHash, RoleCodes.CajeroId, UserStatusCodes.Active,
                i <= 6 ? admin.UserId : gestores[i % 3].UserId, session: i is 1 or 3 or 5))
            .ToList();
        var others = new[]
        {
            NewUser("cajero011", cajeroHash, RoleCodes.CajeroId, UserStatusCodes.Blocked, gestores[0].UserId),
            NewUser("cajero012", cajeroHash, RoleCodes.CajeroId, UserStatusCodes.Inactive, gestores[1].UserId),
            NewUser("cajero013", cajeroHash, RoleCodes.CajeroId, UserStatusCodes.Pending, gestores[0].UserId),
            NewUser("gestor004", gestorHash, RoleCodes.GestorId, UserStatusCodes.Pending, gestores[1].UserId),
        };
        db.Users.AddRange(cajeros);
        db.Users.AddRange(others);
        await db.SaveChangesAsync(ct);

        // ----- Cajas: 10 en total, 2 inactivas -----
        var cashes = await db.Cashes.OrderBy(c => c.CashId).ToListAsync(ct);
        for (var number = cashes.Count + 1; number <= 10; number++)
        {
            var cash = new Cash { CashDescription = $"Caja {number}", Active = number <= 8 };
            cashes.Add(cash);
            db.Cashes.Add(cash);
        }
        await db.SaveChangesAsync(ct);

        // Cajas 1 a 4 con dos cajeros, 5 y 6 con uno; 7 y 8 quedan libres.
        var assignments = new (int Cash, int Cajero)[] { (0, 0), (0, 1), (1, 2), (1, 3), (2, 4), (2, 5), (3, 6), (3, 7), (4, 8), (5, 9) };
        foreach (var (cashIndex, cajeroIndex) in assignments)
        {
            db.UserCashes.Add(new UserCash
            {
                CashId = cashes[cashIndex].CashId,
                UserId = cajeros[cajeroIndex].UserId,
                AssignedBy = gestores[cashIndex % 3].UserId,
                AssignedAt = now.AddDays(-15),
            });
        }

        // La caja 3 está abierta por su primer cajero.
        var openCash = cashes[2];
        var openCashier = cajeros[4];
        openCash.CurrentUserId = openCashier.UserId;
        openCash.OpenedAt = now.AddHours(-2);
        await db.SaveChangesAsync(ct);

        // ----- Clientes -----
        var clients = Enumerable.Range(0, 30).Select(i => new Client
        {
            Name = FirstNames[(i * 7 + 2) % FirstNames.Length],
            LastName = $"{LastNames[(i * 3 + 1) % LastNames.Length]} {LastNames[(i * 11 + 5) % LastNames.Length]}",
            Identification = $"09{20000000 + i * 2468013 % 79999999:D8}",
            Email = $"cliente{i + 1:D2}@correo.demo",
            PhoneNumber = $"09{80000000 + i * 1357913 % 19999999:D8}",
            Address = Streets[i % Streets.Length],
            ReferenceAddress = References[i % References.Length],
            Active = i is not (27 or 28),
            CreationDate = now.AddDays(-random.Next(30, 320)),
        }).ToList();
        db.Clients.AddRange(clients);
        await db.SaveChangesAsync(ct);

        // ----- Contratos y pagos -----
        var services = await db.Services.OrderBy(s => s.SpeedMbps).ToListAsync(ct);
        var methods = await db.MethodPayments.Select(m => m.MethodPaymentId).ToListAsync(ct);

        void AddPayments(Contract contract, Service service, DateTime from, DateTime until)
        {
            var date = from.AddDays(1);
            for (var count = 0; date < until && count < 10; count++, date = date.AddMonths(1))
            {
                db.Payments.Add(new Payment
                {
                    Contract = contract,
                    ClientId = contract.ClientId,
                    Amount = service.Price,
                    MethodPaymentId = contract.MethodPaymentId,
                    PaymentDate = date,
                    UserCashierId = cajeros[random.Next(cajeros.Count)].UserId,
                });
            }
        }

        for (var i = 0; i < 24; i++)
        {
            var client = clients[i];
            var service = services[random.Next(services.Count)];
            var start = now.AddDays(-random.Next(40, 300));
            var contract = new Contract
            {
                ClientId = client.ClientId,
                ServiceId = service.ServiceId,
                MethodPaymentId = methods[random.Next(methods.Count)],
                StatusId = ContractStatusCodes.Vigente,
                StartDate = start,
                EndDate = start.AddYears(1),
            };
            db.Contracts.Add(contract);

            if (i % 8 == 3)
            {
                // Cambio de servicio: el contrato queda sustituido y nace el de renovación.
                var changed = start.AddDays(random.Next(20, 35));
                contract.StatusId = ContractStatusCodes.Sustituido;
                AddPayments(contract, service, start, changed);
                await db.SaveChangesAsync(ct);

                var newService = services.First(s => s.ServiceId != service.ServiceId);
                var renewed = new Contract
                {
                    ClientId = client.ClientId,
                    ServiceId = newService.ServiceId,
                    MethodPaymentId = contract.MethodPaymentId,
                    StatusId = ContractStatusCodes.Renovado,
                    StartDate = changed,
                    EndDate = contract.EndDate,
                    PreviousContractId = contract.ContractId,
                };
                db.Contracts.Add(renewed);
                AddPayments(renewed, newService, changed, now);
            }
            else if (i % 8 == 6)
            {
                // Cancelación: la fecha fin pasa a ser la de la cancelación.
                var cancelled = start.AddDays(random.Next(20, 38));
                contract.StatusId = ContractStatusCodes.Cancelado;
                contract.EndDate = cancelled;
                AddPayments(contract, service, start, cancelled);
            }
            else
            {
                AddPayments(contract, service, start, now);
            }
        }
        await db.SaveChangesAsync(ct);

        // ----- Turnos: la última semana y el día de hoy -----
        var types = await db.AttentionTypes.OrderBy(t => t.AttentionTypeId).ToListAsync(ct);
        var staffed = assignments.GroupBy(a => a.Cash).ToDictionary(g => g.Key, g => g.Select(a => cajeros[a.Cajero]).ToList());

        void AddDay(DateTime firstUtc, DateTime lastUtc, int total, int attended, bool withInProgress)
        {
            var counters = new Dictionary<string, int>();
            var step = (lastUtc - firstUtc) / Math.Max(1, total);
            for (var n = 0; n < total; n++)
            {
                var type = types[random.Next(types.Count)];
                counters[type.Prefix] = counters.GetValueOrDefault(type.Prefix) + 1;

                var inProgress = withInProgress && n == attended;
                var cashIndex = inProgress ? 2 : random.Next(6);
                var isAttended = n < attended;
                var created = firstUtc + step * n;
                var cashier = inProgress ? openCashier : staffed[cashIndex][random.Next(staffed[cashIndex].Count)];

                db.Turns.Add(new Turn
                {
                    Description = $"{type.Prefix}{counters[type.Prefix]:D4}",
                    Date = created,
                    CashId = cashes[cashIndex].CashId,
                    UserGestorId = gestores[n % 3].UserId,
                    AttentionTypeId = type.AttentionTypeId,
                    Attentions =
                    {
                        new Attention
                        {
                            AttentionTypeId = type.AttentionTypeId,
                            StatusId = isAttended ? AttentionStatusCodes.Attended
                                : inProgress ? AttentionStatusCodes.InProgress
                                : AttentionStatusCodes.Pending,
                            UserCashierId = isAttended || inProgress ? cashier.UserId : null,
                            ClientId = isAttended && random.Next(10) < 7 ? clients[random.Next(clients.Count)].ClientId : null,
                            AttendedAt = isAttended ? created.AddMinutes(random.Next(6, 25)) : null,
                        },
                    },
                });
            }
        }

        for (var daysAgo = 6; daysAgo >= 1; daysAgo--)
        {
            var (dayStart, _) = BusinessDay.RangeUtc(BusinessDay.Today.AddDays(-daysAgo));
            var total = random.Next(9, 18);
            AddDay(dayStart.AddHours(9), dayStart.AddHours(17), total, total, withInProgress: false);
        }

        // Hoy: los turnos se reparten entre el inicio de la jornada y este momento.
        var (todayStart, _) = BusinessDay.RangeUtc(BusinessDay.Today);
        var first = todayStart.AddHours(8.5) < now.AddMinutes(-30) ? todayStart.AddHours(8.5) : todayStart.AddMinutes(1);
        AddDay(first, now.AddMinutes(-2), total: 22, attended: 14, withInProgress: true);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Datos de demostración creados: {Users} usuarios, {Cashes} cajas, {Clients} clientes",
            gestores.Count + cajeros.Count + others.Length, cashes.Count, clients.Count);
    }
}
