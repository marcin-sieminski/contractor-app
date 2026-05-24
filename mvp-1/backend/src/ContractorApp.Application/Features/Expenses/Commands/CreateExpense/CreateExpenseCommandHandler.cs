using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Entities;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;

namespace ContractorApp.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateExpenseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExpenseDto> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ExpenseCategory>(request.Category, out var category))
            throw new DomainException($"Nieznana kategoria: {request.Category}");

        if (!Enum.TryParse<Currency>(request.Currency, out var currency))
            throw new DomainException($"Nieznana waluta: {request.Currency}");

        if (currency != Currency.PLN && request.ExchangeRate is null or <= 0)
            throw new DomainException("Kurs wymiany jest wymagany dla walut obcych.");

        var amountPLN = currency == Currency.PLN
            ? request.Amount
            : request.Amount * request.ExchangeRate!.Value;

        var expense = new Expense
        {
            UserId = _currentUser.UserId,
            Date = request.Date,
            Category = category,
            Description = request.Description,
            Amount = request.Amount,
            Currency = currency,
            ExchangeRate = currency == Currency.PLN ? null : request.ExchangeRate,
            AmountPLN = amountPLN,
            IsVatDeductible = request.IsVatDeductible,
            ReceiptNumber = request.ReceiptNumber,
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        return expense.ToDto();
    }
}
