using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class AnnualSettlementConfiguration : IEntityTypeConfiguration<AnnualSettlement>
{
    public void Configure(EntityTypeBuilder<AnnualSettlement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.RevenueOverride).HasPrecision(18, 2);
        builder.Property(e => e.CostsOverride).HasPrecision(18, 2);
        builder.Property(e => e.ZusSocialPaid).HasPrecision(18, 2);
        builder.Property(e => e.ZusHealthPaid).HasPrecision(18, 2);
        builder.Property(e => e.TaxPrepaymentsPaid).HasPrecision(18, 2);
        builder.Property(e => e.IpQualifyingPercent).HasPrecision(5, 2);
        builder.Property(e => e.NexusCoefficient).HasPrecision(5, 4);

        builder.Property(e => e.TaxpayerNip).HasMaxLength(10);
        builder.Property(e => e.FirstName).HasMaxLength(100);
        builder.Property(e => e.LastName).HasMaxLength(100);
        builder.Property(e => e.Street).HasMaxLength(200);
        builder.Property(e => e.BuildingNumber).HasMaxLength(20);
        builder.Property(e => e.ApartmentNumber).HasMaxLength(20);
        builder.Property(e => e.PostalCode).HasMaxLength(6);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.TaxOfficeCode).HasMaxLength(4);
        builder.Property(e => e.Voivodeship).HasMaxLength(60);
        builder.Property(e => e.County).HasMaxLength(60);
        builder.Property(e => e.Commune).HasMaxLength(60);

        builder.Property(e => e.CalculationJson).HasColumnType("jsonb");

        // Jedno rozliczenie na (użytkownik, rok, forma) — pozwala porównywać formy obok siebie.
        builder.HasIndex(e => new { e.UserId, e.Year, e.Form }).IsUnique();
    }
}
