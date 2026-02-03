using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Data;

/// <summary>
/// Database context for the Workflow application with ASP.NET Core Identity integration.
/// Manages entity sets for expense tracking, audit logging, and user authentication.
/// Uses Guid as the primary key for all Identity entities to match domain entities.
/// </summary>
public class WorkflowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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
    /// Applies Identity configurations first, then domain entity configurations.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // CRITICAL: Call base first to configure Identity tables
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);

        // Configure Identity table names to match snake_case convention
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("roles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });
    }
}
