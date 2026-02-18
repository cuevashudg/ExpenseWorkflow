using System;
using Workflow.Domain.Entities;

namespace Workflow.Domain.Entities;

public static class ApplicationUserExtensions
{
    /// <summary>
    /// Returns true if this user is a direct report of the given manager.
    /// </summary>
    public static bool IsDirectReportOf(this ApplicationUser user, ApplicationUser manager)
    {
        return user.ManagerId.HasValue && user.ManagerId.Value == manager.Id;
    }
}
