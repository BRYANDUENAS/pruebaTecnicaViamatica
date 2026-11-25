using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;
using Viamatica.Domain.Entities;

namespace Viamatica.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly IFieldCipher _cipher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IFieldCipher cipher) : base(options)
    {
        _cipher = cipher;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UserStatus> UserStatuses => Set<UserStatus>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RolMenu> RolMenus => Set<RolMenu>();
    public DbSet<Cash> Cashes => Set<Cash>();
    public DbSet<UserCash> UserCashes => Set<UserCash>();
    public DbSet<Turn> Turns => Set<Turn>();
    public DbSet<Attention> Attentions => Set<Attention>();
    public DbSet<AttentionType> AttentionTypes => Set<AttentionType>();
    public DbSet<AttentionStatus> AttentionStatuses => Set<AttentionStatus>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<MethodPayment> MethodPayments => Set<MethodPayment>();
    public DbSet<StatusContract> StatusContracts => Set<StatusContract>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();

    public async Task<int> CreateTurnAsync(string attentionTypeId, int cashId, int gestorId, CancellationToken ct = default)
    {
        var rows = await Database
            .SqlQuery<int>($"SELECT fn_create_turn({attentionTypeId}, {cashId}, {gestorId}) AS \"Value\"")
            .ToListAsync(ct);
        return rows.Single();
    }

    /// <summary>Convierte los borrados físicos en eliminaciones lógicas.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        var encrypted = new ValueConverter<string, string>(v => _cipher.Encrypt(v), v => _cipher.Decrypt(v));

        b.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(20);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(400).HasConversion(encrypted);
            e.Property(x => x.Identification).HasMaxLength(100).HasConversion(encrypted);
            e.Property(x => x.Password).HasMaxLength(100);
            e.Property(x => x.StatusId).HasMaxLength(3);
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId);
            e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Rol>(e =>
        {
            e.HasKey(x => x.RolId);
            e.Property(x => x.RolId).ValueGeneratedNever();
            e.Property(x => x.RolName).HasMaxLength(50);
            e.HasData(
                new Rol { RolId = RoleCodes.AdminId, RolName = RoleCodes.Admin },
                new Rol { RolId = RoleCodes.GestorId, RolName = RoleCodes.Gestor },
                new Rol { RolId = RoleCodes.CajeroId, RolName = RoleCodes.Cajero });
        });

        b.Entity<UserStatus>(e =>
        {
            e.HasKey(x => x.StatusId);
            e.Property(x => x.StatusId).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(50);
            e.HasData(
                new UserStatus { StatusId = UserStatusCodes.Active, Description = "Activo" },
                new UserStatus { StatusId = UserStatusCodes.Inactive, Description = "Inactivo" },
                new UserStatus { StatusId = UserStatusCodes.Blocked, Description = "Bloqueado" },
                new UserStatus { StatusId = UserStatusCodes.Pending, Description = "Pendiente de aprobación" });
        });

        b.Entity<MenuItem>(e =>
        {
            e.HasKey(x => x.MenuItemId);
            e.Property(x => x.MenuItemId).ValueGeneratedNever();
            e.Property(x => x.Label).HasMaxLength(50);
            e.Property(x => x.Route).HasMaxLength(100);
            e.Property(x => x.Icon).HasMaxLength(50);
            e.HasData(
                new MenuItem { MenuItemId = 1, Label = "Inicio", Route = "/inicio", Icon = "home", Order = 1 },
                new MenuItem { MenuItemId = 2, Label = "Dashboard", Route = "/dashboard", Icon = "dashboard", Order = 2 },
                new MenuItem { MenuItemId = 3, Label = "Usuarios", Route = "/usuarios", Icon = "group", Order = 3 },
                new MenuItem { MenuItemId = 4, Label = "Asignación de turnos", Route = "/turnos", Icon = "confirmation_number", Order = 4 },
                new MenuItem { MenuItemId = 5, Label = "Procesos de caja", Route = "/caja", Icon = "point_of_sale", Order = 5 },
                new MenuItem { MenuItemId = 6, Label = "Clientes", Route = "/clientes", Icon = "badge", Order = 6 });
        });

        b.Entity<RolMenu>(e =>
        {
            e.HasKey(x => new { x.RolId, x.MenuItemId });
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId);
            e.HasOne(x => x.MenuItem).WithMany(x => x.Roles).HasForeignKey(x => x.MenuItemId);
            e.HasData(
                new RolMenu { RolId = RoleCodes.AdminId, MenuItemId = 1 },
                new RolMenu { RolId = RoleCodes.AdminId, MenuItemId = 2 },
                new RolMenu { RolId = RoleCodes.AdminId, MenuItemId = 3 },
                new RolMenu { RolId = RoleCodes.GestorId, MenuItemId = 1 },
                new RolMenu { RolId = RoleCodes.GestorId, MenuItemId = 3 },
                new RolMenu { RolId = RoleCodes.GestorId, MenuItemId = 4 },
                new RolMenu { RolId = RoleCodes.CajeroId, MenuItemId = 1 },
                new RolMenu { RolId = RoleCodes.CajeroId, MenuItemId = 5 },
                new RolMenu { RolId = RoleCodes.CajeroId, MenuItemId = 6 });
        });

        b.Entity<Cash>(e =>
        {
            e.HasKey(x => x.CashId);
            e.Property(x => x.CashDescription).HasMaxLength(50);
            e.HasOne(x => x.CurrentUser).WithMany().HasForeignKey(x => x.CurrentUserId);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasData(
                new Cash { CashId = 1, CashDescription = "Caja 1" },
                new Cash { CashId = 2, CashDescription = "Caja 2" },
                new Cash { CashId = 3, CashDescription = "Caja 3" });
        });

        b.Entity<UserCash>(e =>
        {
            e.HasKey(x => new { x.UserId, x.CashId });
            e.HasOne(x => x.User).WithMany(x => x.UserCashes).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Cash).WithMany(x => x.UserCashes).HasForeignKey(x => x.CashId);
            e.HasQueryFilter(x => !x.User.IsDeleted && !x.Cash.IsDeleted);
        });

        b.Entity<AttentionType>(e =>
        {
            e.HasKey(x => x.AttentionTypeId);
            e.Property(x => x.AttentionTypeId).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(100);
            e.Property(x => x.Prefix).HasMaxLength(2).IsFixedLength();
            e.HasIndex(x => x.Prefix).IsUnique();
            e.HasData(
                new AttentionType { AttentionTypeId = AttentionTypeCodes.AtencionCliente, Description = "Atención al cliente", Prefix = "AC" },
                new AttentionType { AttentionTypeId = AttentionTypeCodes.PagoServicio, Description = "Pago de servicio", Prefix = "PS" },
                new AttentionType { AttentionTypeId = AttentionTypeCodes.Contratacion, Description = "Contratación de servicio", Prefix = "CS" },
                new AttentionType { AttentionTypeId = AttentionTypeCodes.CambioServicio, Description = "Cambio de servicio", Prefix = "CM" },
                new AttentionType { AttentionTypeId = AttentionTypeCodes.CambioFormaPago, Description = "Cambio de forma de pago", Prefix = "FP" },
                new AttentionType { AttentionTypeId = AttentionTypeCodes.Cancelacion, Description = "Cancelación de contrato", Prefix = "CC" });
        });

        b.Entity<AttentionStatus>(e =>
        {
            e.HasKey(x => x.StatusId);
            e.Property(x => x.StatusId).ValueGeneratedNever();
            e.Property(x => x.Description).HasMaxLength(30);
            e.HasData(
                new AttentionStatus { StatusId = AttentionStatusCodes.Pending, Description = "Pendiente" },
                new AttentionStatus { StatusId = AttentionStatusCodes.InProgress, Description = "En atención" },
                new AttentionStatus { StatusId = AttentionStatusCodes.Attended, Description = "Atendido" },
                new AttentionStatus { StatusId = AttentionStatusCodes.Cancelled, Description = "Cancelado" });
        });

        b.Entity<Turn>(e =>
        {
            e.HasKey(x => x.TurnId);
            e.Property(x => x.Description).HasMaxLength(6).IsFixedLength();
            e.Property(x => x.AttentionTypeId).HasMaxLength(3);
            e.HasOne(x => x.Cash).WithMany().HasForeignKey(x => x.CashId);
            e.HasOne(x => x.UserGestor).WithMany().HasForeignKey(x => x.UserGestorId);
            e.HasOne(x => x.AttentionType).WithMany().HasForeignKey(x => x.AttentionTypeId);
            e.HasIndex(x => x.Date);
            e.ToTable(t => t.HasCheckConstraint("ck_turns_description", "description ~ '^[A-Z]{2}[0-9]{4}$'"));
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Attention>(e =>
        {
            e.HasKey(x => x.AttentionId);
            e.Property(x => x.AttentionTypeId).HasMaxLength(3);
            e.HasOne(x => x.Turn).WithMany(x => x.Attentions).HasForeignKey(x => x.TurnId);
            e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.AttentionType).WithMany().HasForeignKey(x => x.AttentionTypeId);
            e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId);
            e.HasOne(x => x.UserCashier).WithMany().HasForeignKey(x => x.UserCashierId);
            e.HasQueryFilter(x => !x.Turn.IsDeleted);
        });

        b.Entity<Client>(e =>
        {
            e.HasKey(x => x.ClientId);
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Identification).HasMaxLength(13);
            e.HasIndex(x => x.Identification).IsUnique();
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.PhoneNumber).HasMaxLength(13);
            e.Property(x => x.Address).HasMaxLength(100);
            e.Property(x => x.ReferenceAddress).HasMaxLength(100);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Service>(e =>
        {
            e.HasKey(x => x.ServiceId);
            e.Property(x => x.ServiceName).HasMaxLength(100);
            e.Property(x => x.ServiceDescription).HasMaxLength(150);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasData(
                new Service { ServiceId = 1, ServiceName = "Internet Hogar 100", ServiceDescription = "Plan residencial de fibra óptica", SpeedMbps = 100, Price = 25.00m },
                new Service { ServiceId = 2, ServiceName = "Internet Hogar 300", ServiceDescription = "Plan residencial de fibra óptica de alta velocidad", SpeedMbps = 300, Price = 35.00m },
                new Service { ServiceId = 3, ServiceName = "Internet Pyme 600", ServiceDescription = "Plan para negocios con IP fija", SpeedMbps = 600, Price = 60.00m });
        });

        b.Entity<Device>(e =>
        {
            e.HasKey(x => x.DeviceId);
            e.Property(x => x.DeviceName).HasMaxLength(50);
            e.HasOne(x => x.Service).WithMany(x => x.Devices).HasForeignKey(x => x.ServiceId);
            e.HasQueryFilter(x => !x.Service.IsDeleted);
            e.HasData(
                new Device { DeviceId = 1, DeviceName = "Router WiFi 5", ServiceId = 1 },
                new Device { DeviceId = 2, DeviceName = "Router WiFi 6", ServiceId = 2 },
                new Device { DeviceId = 3, DeviceName = "Extensor de señal", ServiceId = 2 },
                new Device { DeviceId = 4, DeviceName = "Router empresarial WiFi 6", ServiceId = 3 },
                new Device { DeviceId = 5, DeviceName = "ONT de fibra", ServiceId = 3 });
        });

        b.Entity<MethodPayment>(e =>
        {
            e.HasKey(x => x.MethodPaymentId);
            e.Property(x => x.Description).HasMaxLength(50);
            e.HasData(
                new MethodPayment { MethodPaymentId = 1, Description = "Efectivo" },
                new MethodPayment { MethodPaymentId = 2, Description = "Tarjeta de crédito" },
                new MethodPayment { MethodPaymentId = 3, Description = "Tarjeta de débito" },
                new MethodPayment { MethodPaymentId = 4, Description = "Transferencia bancaria" });
        });

        b.Entity<StatusContract>(e =>
        {
            e.HasKey(x => x.StatusId);
            e.Property(x => x.StatusId).HasMaxLength(3);
            e.Property(x => x.Description).HasMaxLength(50);
            e.HasData(
                new StatusContract { StatusId = ContractStatusCodes.Vigente, Description = "Contrato vigente" },
                new StatusContract { StatusId = ContractStatusCodes.Sustituido, Description = "Contrato sustituido" },
                new StatusContract { StatusId = ContractStatusCodes.Renovado, Description = "Renovación de servicio" },
                new StatusContract { StatusId = ContractStatusCodes.Cancelado, Description = "Contrato cancelado" });
        });

        b.Entity<Contract>(e =>
        {
            e.HasKey(x => x.ContractId);
            e.Property(x => x.StatusId).HasMaxLength(3);
            e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
            e.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId);
            e.HasOne(x => x.Client).WithMany(x => x.Contracts).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.MethodPayment).WithMany().HasForeignKey(x => x.MethodPaymentId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Payment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.HasOne(x => x.Client).WithMany(x => x.Payments).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.MethodPayment).WithMany().HasForeignKey(x => x.MethodPaymentId);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
