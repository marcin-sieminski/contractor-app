using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.AmountPLN).HasPrecision(18, 2);
        builder.Property(e => e.ExchangeRate).HasPrecision(10, 6);
        builder.Property(e => e.ReceiptNumber).HasMaxLength(100);
        builder.Property(e => e.VendorName).HasMaxLength(200);
        builder.Property(e => e.VendorNip).HasMaxLength(15);
        builder.Property(e => e.NetAmount).HasPrecision(18, 2);
        builder.Property(e => e.VatAmount).HasPrecision(18, 2);
        builder.Property(e => e.UserId).IsRequired();
        builder.HasQueryFilter(e => e.DeletedAt == null);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Date);
    }
}
