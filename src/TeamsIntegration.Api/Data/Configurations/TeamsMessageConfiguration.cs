using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Data.Configurations;

public class TeamsMessageConfiguration : IEntityTypeConfiguration<TeamsMessage>
{
    public void Configure(
        EntityTypeBuilder<TeamsMessage> builder)
    {
        builder.ToTable("teams_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.GraphMessageId)
            .HasColumnName("graph_message_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasColumnName("team_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ChannelId)
            .HasColumnName("channel_id")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ReplyToId)
            .HasColumnName("reply_to_id")
            .HasMaxLength(512);

        builder.Property(x => x.Subject)
            .HasColumnName("subject")
            .HasMaxLength(1000);

        builder.Property(x => x.HtmlContent)
            .HasColumnName("html_content")
            .HasColumnType("text");

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100);

        builder.Property(x => x.SenderId)
            .HasColumnName("sender_id")
            .HasMaxLength(512);

        builder.Property(x => x.SenderDisplayName)
            .HasColumnName("sender_display_name")
            .HasMaxLength(500);

        builder.Property(x => x.MessageCreatedAt)
            .HasColumnName("message_created_at");

        builder.Property(x => x.MessageLastModifiedAt)
            .HasColumnName("message_last_modified_at");

        builder.Property(x => x.MessageDeletedAt)
            .HasColumnName("message_deleted_at");

        builder.Property(x => x.WebUrl)
            .HasColumnName("web_url")
            .HasColumnType("text");

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
                x.ChannelId,
                x.GraphMessageId
            })
            .IsUnique()
            .HasDatabaseName("ux_teams_messages_team_channel_graph_message");
    }
}
