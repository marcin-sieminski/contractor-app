using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class ExpenseReceiptConfiguration : IEntityTypeConfiguration<ExpenseReceipt>
{
    public void Configure(EntityTypeBuilder<ExpenseReceipt> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.FileName).HasMaxLength(260).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Data).HasColumnType("bytea").IsRequired();
        builder.Property(r => r.Provider).HasMaxLength(20);
        builder.Property(r => r.Model).HasMaxLength(100);
        builder.HasQueryFilter(r => r.DeletedAt == null);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.ExpenseId);
    }
}
