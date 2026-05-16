using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Nip).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(2).IsRequired().HasDefaultValue("PL");
        builder.HasIndex(c => c.Nip);
        builder.HasQueryFilter(c => c.DeletedAt == null);
        builder.HasMany(c => c.Projects).WithOne(p => p.Client).HasForeignKey(p => p.ClientId);
        builder.HasMany(c => c.Invoices).WithOne(i => i.Client).HasForeignKey(i => i.ClientId);
    }
}
