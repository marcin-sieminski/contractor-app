using ContractorApp.Application.DTOs;
using MediatR;

namespace ContractorApp.Application.Features.Expenses.Queries.GetExpenses;

public record GetExpensesQuery(
    DateOnly? From,
    DateOnly? To,
    string? Category) : IRequest<List<ExpenseDto>>;
