using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;

namespace ContractorApp.Application;

public static class MappingExtensions
{
    public static ClientDto ToDto(this Client c) => new(
        c.Id, c.Name, c.Nip, c.Regon, c.Street, c.City, c.PostalCode, c.Country,
        c.IsVerified, c.IsEuVatPayer,
        c.Projects.Where(p => p.DeletedAt == null).Select(p => p.ToDto()).ToList());

    public static ProjectDto ToDto(this Project p) => new(
        p.Id, p.ClientId, p.Name, p.HourlyRate, p.Currency.ToString(), p.IsActive);

    public static TimeEntryDto ToDto(this TimeEntry t, string projectName, string clientName) => new(
        t.Id, t.ProjectId, projectName, clientName,
        t.StartedAt, t.StoppedAt, t.DurationMinutes, t.Description,
        t.IsInvoiced, t.IsRunning, t.IsPaused, t.AccumulatedSeconds);

    public static ExpenseDto ToDto(this Expense e) => new(
        e.Id, e.Date, e.Category.ToString(), e.Description,
        e.Amount, e.Currency.ToString(), e.ExchangeRate, e.AmountPLN,
        e.IsVatDeductible, e.ReceiptNumber,
        e.VendorName, e.VendorNip, e.NetAmount, e.VatAmount,
        e.ReceiptId, e.ReceiptId != null,
        e.CreatedAt);

    public static InvoiceDto ToDto(this Invoice i) => new(
        i.Id, i.InvoiceNumber,
        i.Client?.Name ?? string.Empty,
        i.Client?.Nip ?? string.Empty,
        i.IssueDate, i.ServiceDate, i.DueDate,
        i.Status.ToString(), i.VatTreatment.ToString(), i.Currency.ToString(),
        i.ExchangeRate, i.ExchangeRateDate, i.ExchangeRateTableNumber,
        i.TotalNet, i.TotalVat, i.TotalGross,
        i.KsefReferenceNumber, i.KsefSubmittedAt, i.KsefError,
        i.LineItems.OrderBy(l => l.LineNumber).Select(l => new LineItemDto(
            l.LineNumber, l.Description, l.Quantity, l.Unit,
            l.UnitPrice, l.VatRate, l.NetAmount, l.GrossAmount)).ToList());
}
