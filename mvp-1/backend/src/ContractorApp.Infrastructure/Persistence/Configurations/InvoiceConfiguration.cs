using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.TotalNet).HasPrecision(18, 2);
        builder.Property(i => i.TotalVat).HasPrecision(18, 2);
        builder.Property(i => i.TotalGross).HasPrecision(18, 2);
        builder.Property(i => i.ExchangeRate).HasPrecision(10, 4);
        builder.Property(i => i.XmlContent).HasColumnType("text");
        builder.HasMany(i => i.LineItems).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.TimeEntries).WithOne().HasForeignKey(t => t.InvoiceId).OnDelete(DeleteBehavior.SetNull);
    }
}
