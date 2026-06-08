using ContractorApp.Domain.Common;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Domain.Entities;

public class Expense : Entity
{
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public ExpenseCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.PLN;
    public decimal? ExchangeRate { get; set; }
    public decimal AmountPLN { get; set; }
    public bool IsVatDeductible { get; set; }
    public string? ReceiptNumber { get; set; }

    // Dane sprzedawcy + rozbicie VAT (wypełniane automatycznie z OCR lub ręcznie)
    public string? VendorName { get; set; }
    public string? VendorNip { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? VatAmount { get; set; }

    /// <summary>Powiązany skan paragonu/faktury (<see cref="ExpenseReceipt"/>), jeśli wgrany.</summary>
    public Guid? ReceiptId { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
