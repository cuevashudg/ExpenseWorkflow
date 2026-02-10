using Workflow.Domain.Exceptions;

namespace Workflow.Domain.Entities;

/// <summary>
/// Represents a budget allocation for expense tracking.
/// Can be user-specific or category-specific.
/// </summary>
public class Budget
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    
    /// <summary>
    /// If set, budget applies only to this user. If null, applies to all users.
    /// </summary>
    public Guid? UserId { get; private set; }
    
    /// <summary>
    /// If set, budget applies only to this category. If null, applies to all categories.
    /// </summary>
    public Guid? CategoryId { get; private set; }
    public ExpenseCategory? Category { get; private set; }
    
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core
    private Budget() 
    {
        Name = string.Empty;
    }

    public Budget(string name, decimal amount, DateTime startDate, DateTime endDate, 
        string? description = null, Guid? userId = null, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Budget name cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Budget amount must be greater than zero.");

        if (startDate >= endDate)
            throw new DomainException("Start date must be before end date.");

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Amount = amount;
        StartDate = startDate;
        EndDate = endDate;
        UserId = userId;
        CategoryId = categoryId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, decimal amount, DateTime startDate, DateTime endDate, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Budget name cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Budget amount must be greater than zero.");

        if (startDate >= endDate)
            throw new DomainException("Start date must be before end date.");

        Name = name;
        Description = description;
        Amount = amount;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this budget is currently in effect based on the date range.
    /// </summary>
    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }
}
