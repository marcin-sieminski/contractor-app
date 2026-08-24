using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class TaxPaymentConfiguration : IEntityTypeConfiguration<TaxPayment>
{
    public void Configure(EntityTypeBuilder<TaxPayment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.HasIndex(e => new { e.UserId, e.Year, e.Month, e.Type });
    }
}
