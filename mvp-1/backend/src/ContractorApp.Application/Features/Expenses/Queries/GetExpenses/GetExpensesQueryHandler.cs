using ContractorApp.Application.Common.Interfaces;
using ContractorApp.Application.DTOs;
using ContractorApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Features.Expenses.Queries.GetExpenses;

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, List<ExpenseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExpensesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Expenses.Where(e => e.UserId == _currentUser.UserId);

        if (request.From.HasValue)
            query = query.Where(e => e.Date >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(e => e.Date <= request.To.Value);

        if (!string.IsNullOrWhiteSpace(request.Category) &&
            Enum.TryParse<ExpenseCategory>(request.Category, out var cat))
            query = query.Where(e => e.Category == cat);

        return await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => e.ToDto())
            .ToListAsync(cancellationToken);
    }
}
