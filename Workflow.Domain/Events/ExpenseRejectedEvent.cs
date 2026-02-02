namespace Workflow.Domain.Events;

/// <summary>
/// Domain event raised when an expense request is rejected by a manager.
/// </summary>
public class ExpenseRejectedEvent : IDomainEvent
{
    public Guid ExpenseRequestId { get; }
    public Guid RejectedBy { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }

    public ExpenseRejectedEvent(Guid expenseRequestId, Guid rejectedBy, string reason)
    {
        ExpenseRequestId = expenseRequestId;
        RejectedBy = rejectedBy;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }
}
