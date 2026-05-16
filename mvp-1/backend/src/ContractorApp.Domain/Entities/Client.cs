using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

public class Client : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Nip { get; set; } = string.Empty;
    public string? Regon { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "PL";
    public bool IsVerified { get; set; }
    public bool IsEuVatPayer { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
