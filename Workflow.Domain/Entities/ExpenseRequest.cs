using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Domain.Entities;

public class ExpenseRequest
{
    public Guid Id { get; private set; }
    public Guid CreatorId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid? ProcessedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    // Private constructor for EF Core
    private ExpenseRequest() { }

    public ExpenseRequest(Guid creatorId, string title, string description, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        Id = Guid.NewGuid();
        CreatorId = creatorId;
        Title = title;
        Description = description;
        Amount = amount;
        Status = ExpenseStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    // Business Rule: Only creator can edit Draft
    public void Update(Guid userId, string title, string description, decimal amount)
    {
        if (Status != ExpenseStatus.Draft)
            throw new DomainException("Only draft requests can be edited.");

        if (userId != CreatorId)
            throw new DomainException("Only the creator can edit this request.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        Title = title;
        Description = description;
        Amount = amount;
    }

    // Business Rule: Only drafts can be submitted
    public void Submit(Guid userId)
    {
        if (Status != ExpenseStatus.Draft)
            throw new DomainException("Only drafts can be submitted.");

        if (userId != CreatorId)
            throw new DomainException("Only the creator can submit this request.");

        Status = ExpenseStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    // Business Rule: Only Manager can approve/reject, only Submitted requests can be processed
    public void Approve(Guid managerId, UserRole userRole)
    {
        if (userRole != UserRole.Manager && userRole != UserRole.Admin)
            throw new DomainException("Only managers or admins can approve requests.");

        if (Status != ExpenseStatus.Submitted)
            throw new DomainException("Only submitted requests can be approved.");

        Status = ExpenseStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
        ProcessedBy = managerId;
    }

    // Business Rule: Only Manager can approve/reject, only Submitted requests can be processed
    public void Reject(Guid managerId, UserRole userRole, string reason)
    {
        if (userRole != UserRole.Manager && userRole != UserRole.Admin)
            throw new DomainException("Only managers or admins can reject requests.");

        if (Status != ExpenseStatus.Submitted)
            throw new DomainException("Only submitted requests can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        Status = ExpenseStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        ProcessedBy = managerId;
        RejectionReason = reason;
    }

    // Business Rule: Approved requests cannot change
    public void EnsureNotApproved()
    {
        if (Status == ExpenseStatus.Approved)
            throw new DomainException("Approved requests cannot be modified.");
    }

    // Business Rule: Rejected requests cannot be resubmitted
    public void EnsureNotRejected()
    {
        if (Status == ExpenseStatus.Rejected)
            throw new DomainException("Rejected requests cannot be resubmitted.");
    }
}
