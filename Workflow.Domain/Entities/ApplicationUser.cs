using Microsoft.AspNetCore.Identity;
using Workflow.Domain.Enums;

namespace Workflow.Domain.Entities;

/// <summary>
/// Represents a user in the system with Identity integration.
/// Uses Guid as the primary key to match ExpenseRequest.CreatorId.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// The role of the user in the system (Employee, Manager, Admin).
    /// This supplements Identity's role system with domain-specific roles.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
