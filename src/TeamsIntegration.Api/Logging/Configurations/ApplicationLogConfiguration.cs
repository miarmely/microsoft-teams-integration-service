using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamsIntegration.Api.Logging.Entities;

namespace TeamsIntegration.Api.Logging.Configurations;

public class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
{
    public void Configure(EntityTypeBuilder<ApplicationLog> builder)
    {
        builder.ToTable("application_logs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(log => log.Level)
            .HasColumnName("level")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Category)
            .HasColumnName("category")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(log => log.EventId)
            .HasColumnName("event_id");

        builder.Property(log => log.EventName)
            .HasColumnName("event_name")
            .HasMaxLength(200);

        builder.Property(log => log.Message)
            .HasColumnName("message")
            .HasMaxLength(4000);

        builder.Property(log => log.ExceptionType)
            .HasColumnName("exception_type")
            .HasMaxLength(500);

        builder.Property(log => log.ExceptionMessage)
            .HasColumnName("exception_message")
            .HasMaxLength(4000);

        builder.Property(log => log.StackTrace)
            .HasColumnName("stack_trace")
            .HasMaxLength(4000);

        builder.Property(log => log.TraceId)
            .HasColumnName("trace_id")
            .HasMaxLength(100);

        builder.Property(log => log.SpanId)
            .HasColumnName("span_id")
            .HasMaxLength(100);

        builder.Property(log => log.RequestPath)
            .HasColumnName("request_path")
            .HasMaxLength(1000);

        builder.Property(log => log.HttpMethod)
            .HasColumnName("http_method")
            .HasMaxLength(10);

        builder.Property(log => log.PropertiesJson)
            .HasColumnName("properties_json")
            .HasColumnType("jsonb");

        builder.Property(log => log.Environment)
            .HasColumnName("environment")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(log => log.MachineName)
            .HasColumnName("machine_name")
            .HasMaxLength(200)
            .IsRequired();
    }
}
