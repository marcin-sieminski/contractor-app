using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class ForecastOverrideConfiguration : IEntityTypeConfiguration<ForecastOverride>
{
    public void Configure(EntityTypeBuilder<ForecastOverride> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Revenue).HasPrecision(18, 2);
        builder.Property(e => e.Cost).HasPrecision(18, 2);
        // Jeden wpis na (użytkownik, rok, miesiąc) — pozwala na upsert korekt.
        builder.HasIndex(e => new { e.UserId, e.Year, e.Month }).IsUnique();
    }
}
