using Workflow.Domain.Enums;

namespace Workflow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid ExpenseRequestId { get; private set; }
    public Guid UserId { get; private set; }
    public string Action { get; private set; }
    public ExpenseStatus? PreviousStatus { get; private set; }
    public ExpenseStatus? NewStatus { get; private set; }
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Private constructor for EF Core
    private AuditLog() { }

    public AuditLog(
        Guid expenseRequestId, 
        Guid userId, 
        string action, 
        ExpenseStatus? previousStatus = null,
        ExpenseStatus? newStatus = null,
        string? details = null)
    {
        Id = Guid.NewGuid();
        ExpenseRequestId = expenseRequestId;
        UserId = userId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }

    public static AuditLog ForCreation(Guid expenseRequestId, Guid userId)
    {
        return new AuditLog(
            expenseRequestId,
            userId,
            "Created",
            newStatus: ExpenseStatus.Draft,
            details: "Expense request created");
    }

    public static AuditLog ForUpdate(Guid expenseRequestId, Guid userId, string changes)
    {
        return new AuditLog(
            expenseRequestId,
            userId,
            "Updated",
            details: changes);
    }

    public static AuditLog ForStatusChange(
        Guid expenseRequestId, 
        Guid userId, 
        ExpenseStatus previousStatus, 
        ExpenseStatus newStatus,
        string? reason = null)
    {
        return new AuditLog(
            expenseRequestId,
            userId,
            "StatusChanged",
            previousStatus,
            newStatus,
            reason);
    }
}
