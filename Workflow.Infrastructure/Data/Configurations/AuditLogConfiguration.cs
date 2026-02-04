using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the AuditLog entity.
/// Defines table schema and constraints for audit trail.
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table configuration
        builder.ToTable("audit_logs");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.ExpenseRequestId)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PreviousStatus)
            .HasConversion<string>(); // Store enum as string

        builder.Property(a => a.NewStatus)
            .HasConversion<string>(); // Store enum as string

        builder.Property(a => a.Details)
            .HasMaxLength(2000);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(a => a.ExpenseRequestId)
            .HasDatabaseName("IX_audit_logs_expense_request_id");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_audit_logs_user_id");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_audit_logs_timestamp");
    }
}
