using ContractorApp.Domain.Common;

namespace ContractorApp.Domain.Entities;

public class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "godz.";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }

    public Invoice Invoice { get; set; } = null!;
}
