using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Data.Configurations;

public sealed class WebhookUrlConfiguration : IEntityTypeConfiguration<WebhookUrl>
{
    public void Configure(
        EntityTypeBuilder<WebhookUrl> builder)
    {
        builder.ToTable("webhook_urls");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
           .HasColumnName("team_id")
           .HasMaxLength(512)
           .IsRequired();

        builder.Property(x => x.ChannelId)
            .HasColumnName("channel_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasColumnName("url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
             .HasColumnName("created_at")
             .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder
            .HasIndex(x => new
            {
                x.TeamId,
                x.ChannelId
            })
            .IsUnique()
            .HasDatabaseName("ux_webhook_url_team_channel");
    }
}
