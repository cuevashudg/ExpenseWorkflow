using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Domain.Entities;

public class ExpenseRequest
{
    public UserRole CreatorRole { get; private set; } // Add this property for role tracking
    public Guid Id { get; private set; }
    public Guid CreatorId { get; private set; }
    public string? CreatorName { get; set; } // Populated by service layer
    public Guid? CategoryId { get; private set; }
    public ExpenseCategory? Category { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid? ProcessedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    
    private readonly List<string> _attachmentUrls = new();
    public IReadOnlyCollection<string> AttachmentUrls => _attachmentUrls.AsReadOnly();

    // Private constructor for EF Core
    private ExpenseRequest() 
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    public ExpenseRequest(Guid creatorId, string title, string description, decimal amount, DateTime expenseDate, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        if (expenseDate > DateTime.UtcNow)
            throw new DomainException("Expense date cannot be in the future.");

        Id = Guid.NewGuid();
        CreatorId = creatorId;
        Title = title;
        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        Status = ExpenseStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        CreatorRole = UserRole.Employee; // Default, should be set by service layer
    }

    // Business Rule: Only creator can edit Draft
    public void Update(Guid userId, string title, string description, decimal amount, Guid? categoryId = null)
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
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    // Business Rule: Only drafts can be submitted
    // Business Rule: Expenses > $100 require receipt
    public void Submit(Guid userId)
    {
        if (Status != ExpenseStatus.Draft)
            throw new DomainException("Only drafts can be submitted.");

        if (userId != CreatorId)
            throw new DomainException("Only the creator can submit this request.");

        if (Amount > 100 && _attachmentUrls.Count == 0)
            throw new DomainException("Expenses over $100 require a receipt attachment.");

        Status = ExpenseStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    // Business Rule: Only Manager/Admin can approve/reject, only Submitted requests can be processed
    public void Approve(Guid managerId, UserRole userRole)
    {
        if (userRole != UserRole.Manager && userRole != UserRole.Admin)
            throw new DomainException("Only managers or admins can approve requests.");

        if (Status != ExpenseStatus.Submitted)
            throw new DomainException("Only submitted requests can be approved.");

        // Exception 1: If manager submits, only admin can approve
        if (userRole == UserRole.Manager && CreatorId == managerId)
            throw new DomainException("Managers cannot approve their own expenses. Only admins can approve manager expenses.");

        // Exception 2: If submitter is a manager, only admin can approve
        if (userRole == UserRole.Manager && CreatorId != managerId && CreatorRole == UserRole.Manager)
            throw new DomainException("Managers cannot approve other managers' expenses. Only admins can approve manager expenses.");

        // Exception 3: If amount exceeds threshold, only admin can approve
        decimal approvalThreshold = 1000m; // Set your threshold here
        if (Amount > approvalThreshold && userRole != UserRole.Admin)
            throw new DomainException($"Expenses over ${approvalThreshold} require admin approval.");

        Status = ExpenseStatus.Approved;
        ProcessedAt = DateTime.UtcNow;
        ProcessedBy = managerId;
    }

    // Business Rule: Only Manager/Admin can approve/reject, only Submitted requests can be processed
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

    // Business Rule: Only draft expenses can have attachments added
    public void AddAttachment(string attachmentUrl)
    {
        if (Status != ExpenseStatus.Draft)
            throw new DomainException("Only draft requests can have attachments added.");

        if (string.IsNullOrWhiteSpace(attachmentUrl))
            throw new DomainException("Attachment URL cannot be empty.");

        _attachmentUrls.Add(attachmentUrl);
        UpdatedAt = DateTime.UtcNow;
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
