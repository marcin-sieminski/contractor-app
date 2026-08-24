using MediatR;

namespace ContractorApp.Application.Features.Expenses.Queries.GetExpenseReceipt;

/// <summary>Pobiera plik skanu powiązanego z danym wydatkiem (do podglądu). Zwraca null, jeśli brak.</summary>
public record GetExpenseReceiptQuery(Guid ExpenseId) : IRequest<ReceiptFileResult?>;

public record ReceiptFileResult(byte[] Data, string ContentType, string FileName);
