using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Expenses.Commands.DeleteExpense;

public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteExpenseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new DomainException("Wydatek nie istnieje.");

        expense.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
