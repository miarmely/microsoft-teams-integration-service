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
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.BucketName)
            .HasColumnName("bucket_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ObjectName)
            .HasColumnName("object_name")
            .HasMaxLength(1500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(x => x.ETag)
            .HasColumnName("e_tag")
            .HasMaxLength(200);

        builder.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
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
            .HasDatabaseName("ux_message_media_hosted_content");

        builder
            .HasIndex(x => new
            {
                x.BucketName,
                x.ObjectName
            })
            .IsUnique()
            .HasDatabaseName("ux_bucket_name_object_name");
    }
}
