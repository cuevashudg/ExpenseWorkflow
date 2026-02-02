using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Data;

/// <summary>
/// Database context for the Workflow application.
/// Manages entity sets and database configuration for expense tracking and audit logging.
/// </summary>
public class WorkflowDbContext : DbContext
{
    /// <summary>
    /// Gets the DbSet for ExpenseRequest entities.
    /// Represents the expense_requests table in the database.
    /// </summary>
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();

    /// <summary>
    /// Gets the DbSet for AuditLog entities.
    /// Represents the audit_logs table in the database.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Initializes a new instance of the WorkflowDbContext.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
        : base(options) { }

    /// <summary>
    /// Configures the entity mappings and database schema.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }
}
