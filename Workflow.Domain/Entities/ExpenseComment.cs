namespace Workflow.Domain.Entities;

/// <summary>
/// Represents a comment on an expense request.
/// </summary>
public class ExpenseComment
{
    public Guid Id { get; set; }
    public Guid ExpenseRequestId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
