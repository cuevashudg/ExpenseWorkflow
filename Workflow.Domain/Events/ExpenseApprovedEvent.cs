namespace Workflow.Domain.Events;

/// <summary>
/// Domain event raised when an expense request is approved by a manager.
/// </summary>
public class ExpenseApprovedEvent : IDomainEvent
{
    public Guid ExpenseRequestId { get; }
    public Guid ApprovedBy { get; }
    public decimal Amount { get; }
    public DateTime OccurredOn { get; }

    public ExpenseApprovedEvent(Guid expenseRequestId, Guid approvedBy, decimal amount)
    {
        ExpenseRequestId = expenseRequestId;
        ApprovedBy = approvedBy;
        Amount = amount;
        OccurredOn = DateTime.UtcNow;
    }
}
