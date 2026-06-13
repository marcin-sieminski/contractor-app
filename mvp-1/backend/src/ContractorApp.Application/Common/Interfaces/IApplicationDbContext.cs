using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContractorApp.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<Project> Projects { get; }
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseReceipt> ExpenseReceipts { get; }
    DbSet<ForecastOverride> ForecastOverrides { get; }
    DbSet<AnnualSettlement> AnnualSettlements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
