using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasQueryFilter(t => t.DeletedAt == null);
        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.IsInvoiced);
        // Enforce only one running timer at a time via unique partial index
        builder.HasIndex(t => t.StoppedAt)
            .HasFilter("\"StoppedAt\" IS NULL")
            .IsUnique();
    }
}
