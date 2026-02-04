namespace Workflow.Domain.Events;

/// <summary>
/// Domain event raised when an expense request is submitted for approval.
/// </summary>
public class ExpenseSubmittedEvent : IDomainEvent
{
    public Guid ExpenseRequestId { get; }
    public Guid SubmittedBy { get; }
    public decimal Amount { get; }
    public DateTime OccurredOn { get; }

    public ExpenseSubmittedEvent(Guid expenseRequestId, Guid submittedBy, decimal amount)
    {
        ExpenseRequestId = expenseRequestId;
        SubmittedBy = submittedBy;
        Amount = amount;
        OccurredOn = DateTime.UtcNow;
    }
}
