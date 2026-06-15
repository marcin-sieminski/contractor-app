using System.ComponentModel;
using ContractorApp.Application.Features.Expenses.Commands.CreateExpense;
using ContractorApp.Application.Features.Expenses.Commands.DeleteExpense;
using ContractorApp.Application.Features.Expenses.Commands.UpdateExpense;
using ContractorApp.Application.Features.Expenses.Queries.GetExpenses;
using ContractorApp.Mcp.Api.Services.Ai;
using MediatR;
using ModelContextProtocol.Server;

namespace ContractorApp.Mcp.Api.McpTools;

[McpServerToolType]
public class ExpenseTools(ISender mediator)
{
    [McpServerTool(Name = "get_expenses")]
    [Description("Returns expense records. Optionally filter by date range (ISO 8601) and/or category (Software/Hardware/Office/Training/Travel/Phone/Insurance/Accounting/Marketing/Other).")]
    public async Task<object> GetExpenses(
        [Description("Start date, e.g. '2025-01-01'")] string? from,
        [Description("End date, e.g. '2025-12-31'")] string? to,
        [Description("Category filter, e.g. 'Software'")] string? category,
        CancellationToken ct)
        => await mediator.Send(
            new GetExpensesQuery(
                from is not null ? DateOnly.Parse(from) : null,
                to is not null ? DateOnly.Parse(to) : null,
                category),
            ct);

    [McpServerTool(Name = "create_expense")]
    [Description("Dodaje wydatek (koszt). Kategorie: Software/Hardware/Office/Training/Travel/Phone/" +
                 "Insurance/Accounting/Marketing/Other. Waluta domyślnie PLN, VAT domyślnie odliczalny.")]
    public async Task<object> CreateExpense(
        [Description("Data wydatku ISO 8601, np. '2025-05-10'.")] string date,
        [Description("Kategoria, np. 'Software'.")] string category,
        [Description("Opis wydatku.")] string description,
        [Description("Kwota brutto.")] decimal amount,
        [Description("Waluta (kod ISO, np. PLN, EUR). Domyślnie PLN.")] string? currency,
        [Description("Czy VAT podlega odliczeniu. Domyślnie true.")] bool? isVatDeductible,
        [Description("Nazwa sprzedawcy (opcjonalnie).")] string? vendorName,
        [Description("NIP sprzedawcy (opcjonalnie).")] string? vendorNip,
        [Description("Numer dokumentu/paragonu (opcjonalnie).")] string? receiptNumber,
        CancellationToken ct)
        => await mediator.Send(new CreateExpenseCommand(
            Date: DateOnly.Parse(date),
            Category: category,
            Description: description,
            Amount: amount,
            Currency: currency ?? "PLN",
            ExchangeRate: null,
            IsVatDeductible: isVatDeductible ?? true,
            ReceiptNumber: receiptNumber,
            VendorName: vendorName,
            VendorNip: vendorNip), ct);

    [McpServerTool(Name = "update_expense")]
    [Description("Aktualizuje wydatek (po GUID). Pola pominięte pozostają bez zmian.")]
    public async Task<object> UpdateExpense(
        [Description("GUID wydatku z get_expenses.")] Guid expenseId,
        [Description("Nowa data ISO 8601 (opcjonalnie).")] string? date,
        [Description("Nowa kategoria (opcjonalnie).")] string? category,
        [Description("Nowy opis (opcjonalnie).")] string? description,
        [Description("Nowa kwota brutto (opcjonalnie).")] decimal? amount,
        [Description("Nowa waluta (opcjonalnie).")] string? currency,
        [Description("Czy VAT odliczalny (opcjonalnie).")] bool? isVatDeductible,
        CancellationToken ct)
    {
        var expenses = await mediator.Send(new GetExpensesQuery(null, null, null), ct);
        var current = expenses.FirstOrDefault(e => e.Id == expenseId)
            ?? throw new InvalidOperationException("Nie znaleziono wydatku o podanym GUID.");

        return await mediator.Send(new UpdateExpenseCommand(
            Id: expenseId,
            Date: date is not null ? DateOnly.Parse(date) : current.Date,
            Category: category ?? current.Category,
            Description: description ?? current.Description,
            Amount: amount ?? current.Amount,
            Currency: currency ?? current.Currency,
            ExchangeRate: current.ExchangeRate,
            IsVatDeductible: isVatDeductible ?? current.IsVatDeductible,
            ReceiptNumber: current.ReceiptNumber,
            VendorName: current.VendorName,
            VendorNip: current.VendorNip,
            NetAmount: current.NetAmount,
            VatAmount: current.VatAmount,
            ReceiptId: current.ReceiptId), ct);
    }

    [McpServerTool(Name = "delete_expense")]
    [RequiresConfirmation("Usunięcie wydatku")]
    [Description("Usuwa wydatek po GUID. Wymaga potwierdzenia. GUID pobierz z get_expenses.")]
    public async Task<object> DeleteExpense(
        [Description("GUID wydatku z get_expenses.")] Guid expenseId,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteExpenseCommand(expenseId), ct);
        return new { deleted = true, expenseId };
    }
}
