using ContractorApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractorApp.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.HasIndex(e => new { e.UserId, e.UpdatedAt });

        builder.HasMany(e => e.Messages)
            .WithOne(m => m.Conversation!)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Content).IsRequired();
        builder.HasIndex(e => e.ConversationId);
    }
}
