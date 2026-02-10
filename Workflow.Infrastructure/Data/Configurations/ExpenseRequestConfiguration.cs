using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for the ExpenseRequest entity.
/// Defines table schema, relationships, and constraints.
/// </summary>
public class ExpenseRequestConfiguration : IEntityTypeConfiguration<ExpenseRequest>
{
    public void Configure(EntityTypeBuilder<ExpenseRequest> builder)
    {
        // Table configuration
        builder.ToTable("expense_requests");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.CreatorId)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Amount)
            .IsRequired()
            .HasPrecision(18, 2); // Decimal precision for money

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string in database

        builder.Property(e => e.ExpenseDate)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.SubmittedAt);

        builder.Property(e => e.ProcessedAt);

        builder.Property(e => e.ProcessedBy);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatorName)
            .HasMaxLength(200);

        // Backing field for private collection
        builder.Property<List<string>>("_attachmentUrls")
            .HasColumnName("attachment_urls")
            .HasConversion(
                v => string.Join(';', v), // Convert list to string for storage
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()) // Convert back to list
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()));

        // Indexes for common queries
        builder.HasIndex(e => e.CreatorId)
            .HasDatabaseName("IX_expense_requests_creator_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_expense_requests_status");

        builder.HasIndex(e => e.ExpenseDate)
            .HasDatabaseName("IX_expense_requests_expense_date");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_expense_requests_created_at");
    }
}
