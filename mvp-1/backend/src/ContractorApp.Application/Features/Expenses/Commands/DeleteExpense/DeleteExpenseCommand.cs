using MediatR;

namespace ContractorApp.Application.Features.Expenses.Commands.DeleteExpense;

public record DeleteExpenseCommand(Guid Id) : IRequest;
