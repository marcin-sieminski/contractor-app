using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Enums;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Expenses.Commands.UpdateExpense;

public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, ExpenseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateExpenseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ExpenseDto> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new DomainException("Wydatek nie istnieje.");

        if (!Enum.TryParse<ExpenseCategory>(request.Category, out var category))
            throw new DomainException($"Nieznana kategoria: {request.Category}");

        if (!Enum.TryParse<Currency>(request.Currency, out var currency))
            throw new DomainException($"Nieznana waluta: {request.Currency}");

        if (currency != Currency.PLN && request.ExchangeRate is null or <= 0)
            throw new DomainException("Kurs wymiany jest wymagany dla walut obcych.");

        expense.Date = request.Date;
        expense.Category = category;
        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.Currency = currency;
        expense.ExchangeRate = currency == Currency.PLN ? null : request.ExchangeRate;
        expense.AmountPLN = currency == Currency.PLN ? request.Amount : request.Amount * request.ExchangeRate!.Value;
        expense.IsVatDeductible = request.IsVatDeductible;
        expense.ReceiptNumber = request.ReceiptNumber;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return expense.ToDto();
    }
}
