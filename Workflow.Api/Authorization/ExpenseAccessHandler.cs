using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Api.Authorization;

public interface IUserLookupService
{
    Task<ApplicationUser?> GetUserByIdAsync(Guid userId);
}

/// <summary>
/// Handles resource-based authorization for expense access.
/// </summary>
public class ExpenseAccessHandler : AuthorizationHandler<ExpenseAccessRequirement, ExpenseRequest>
{
    private readonly IUserLookupService _userLookup;

    public ExpenseAccessHandler(IUserLookupService userLookup)
    {
        _userLookup = userLookup;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ExpenseAccessRequirement requirement,
        ExpenseRequest expense)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = context.User.FindFirstValue(ClaimTypes.Role);

        // Defensive: Check for missing/empty claims
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            return;

        // Defensive: Validate userId is a valid GUID
        if (!Guid.TryParse(userId, out var userGuid))
            return;

        // Defensive: Validate userRole is a known enum value
        if (!Enum.TryParse<UserRole>(userRole, out var parsedRole))
            return;

        if (parsedRole == UserRole.Admin)
        {
            context.Succeed(requirement);
            return;
        }

        if (parsedRole == UserRole.Employee && expense.CreatorId == userGuid)
        {
            context.Succeed(requirement);
            return;
        }

        if (parsedRole == UserRole.Manager)
        {
            var creator = await _userLookup.GetUserByIdAsync(expense.CreatorId);
            // Defensive: Check creator exists and is active (if IsActive property exists)
            if (creator == null)
                return;
            var isActiveProp = creator.GetType().GetProperty("IsActive");
            if (isActiveProp != null && isActiveProp.PropertyType == typeof(bool))
            {
                var isActive = (bool)isActiveProp.GetValue(creator)!;
                if (!isActive)
                    return;
            }
            if (creator.ManagerId.HasValue && creator.ManagerId.Value == userGuid)
            {
                context.Succeed(requirement);
                return;
            }
        }
        // else: do not call Succeed
    }
}
