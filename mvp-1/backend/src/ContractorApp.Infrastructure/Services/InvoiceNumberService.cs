using ContractorApp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Infrastructure.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly ContractorApp.Infrastructure.Persistence.ApplicationDbContext _db;

    public InvoiceNumberService(ContractorApp.Infrastructure.Persistence.ApplicationDbContext db) => _db = db;

    public async Task<string> GenerateNextAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var count = await _db.Invoices
            .CountAsync(i => i.IssueDate.Year == year && i.IssueDate.Month == month, cancellationToken);

        return $"FV/{year}/{month:D2}/{(count + 1):D3}";
    }
}
