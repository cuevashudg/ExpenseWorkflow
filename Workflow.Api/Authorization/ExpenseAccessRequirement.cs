using Microsoft.AspNetCore.Authorization;

namespace Workflow.Api.Authorization;

/// <summary>
/// Requirement for authorizing access to an expense or its attachments.
/// Succeeds if the user is:
/// - An Admin (can access any expense)
/// - The Employee who created the expense
/// - A Manager of the employee who created the expense
/// </summary>
public class ExpenseAccessRequirement : IAuthorizationRequirement
{
    // Marker class; logic is implemented in the handler.
}
