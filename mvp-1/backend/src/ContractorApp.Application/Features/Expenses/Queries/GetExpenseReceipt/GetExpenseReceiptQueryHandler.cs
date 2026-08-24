using ContractorApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Expenses.Queries.GetExpenseReceipt;

public class GetExpenseReceiptQueryHandler : IRequestHandler<GetExpenseReceiptQuery, ReceiptFileResult?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExpenseReceiptQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReceiptFileResult?> Handle(GetExpenseReceiptQuery request, CancellationToken cancellationToken)
    {
        var receipt = await _db.ExpenseReceipts
            .Where(r => r.ExpenseId == request.ExpenseId && r.UserId == _currentUser.UserId)
            .Select(r => new ReceiptFileResult(r.Data, r.ContentType, r.FileName))
            .FirstOrDefaultAsync(cancellationToken);

        return receipt;
    }
}
