using ContractorApp.Domain.Common;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Domain.Entities;

public class Invoice : Entity
{
    public Guid ClientId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly ServiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public VatTreatment VatTreatment { get; set; } = VatTreatment.Domestic23;
    public Currency Currency { get; set; } = Currency.PLN;
    public decimal? ExchangeRate { get; set; }
    public DateOnly? ExchangeRateDate { get; set; }
    public string? ExchangeRateTableNumber { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }
    public string? KsefReferenceNumber { get; set; }
    public DateTimeOffset? KsefSubmittedAt { get; set; }
    public string? KsefError { get; set; }
    public string? XmlContent { get; set; }

    public Client Client { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}
