using Viamatica.Domain.Common;

namespace Viamatica.Domain.Entities;

public class Client : ISoftDelete
{
    public int ClientId { get; set; }
    public string Name { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string ReferenceAddress { get; set; } = null!;
    public bool Active { get; set; } = true;
    public DateTime CreationDate { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Service : ISoftDelete
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = null!;
    public string ServiceDescription { get; set; } = null!;
    public int SpeedMbps { get; set; }
    public decimal Price { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}

public class Device
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}

public class MethodPayment
{
    public int MethodPaymentId { get; set; }
    public string Description { get; set; } = null!;
}

public class StatusContract
{
    public string StatusId { get; set; } = null!;
    public string Description { get; set; } = null!;
}

public class Contract : ISoftDelete
{
    public int ContractId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public string StatusId { get; set; } = ContractStatusCodes.Vigente;
    public StatusContract Status { get; set; } = null!;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int MethodPaymentId { get; set; }
    public MethodPayment MethodPayment { get; set; } = null!;

    /// <summary>Contrato al que reemplaza cuando hubo cambio de servicio.</summary>
    public int? PreviousContractId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class Payment : ISoftDelete
{
    public int PaymentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public int MethodPaymentId { get; set; }
    public MethodPayment MethodPayment { get; set; } = null!;

    public int UserCashierId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
