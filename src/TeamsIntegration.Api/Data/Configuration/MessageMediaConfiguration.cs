using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Data.Configuration;

public sealed class MessageMediaConfiguration : IEntityTypeConfiguration<MessageMedia>
{
    public void Configure(
        EntityTypeBuilder<MessageMedia> builder)
    {
        builder.ToTable("message_media");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.GraphHostedContentId)
            .HasColumnName("graph_hosted_content_id")
            .HasMaxLength(512);

        builder.Property(x => x.GraphAttachmentId)
            .HasColumnName("graph_attachment_id")
            .HasMaxLength(512);

        builder.Property(x => x.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FileExtension)
            .HasColumnName("file_extension")
            .HasMaxLength(50);

        builder.Property(x => x.RelativePath)
            .HasColumnName("relative_path")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .HasColumnName("file_size");

        builder.Property(x => x.Checksum)
            .HasColumnName("checksum")
            .HasMaxLength(128);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.TeamsMessage)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.TeamsMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new
            {
                x.TeamsMessageId,
                x.GraphHostedContentId
            })
            .IsUnique()
            .HasFilter("\"graph_hosted_content_id\" IS NOT NULL")
            .HasDatabaseName("ux_message_media_hosted_content");
    }

}
