using Microsoft.EntityFrameworkCore;
using Workflow.Application.Models;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Data;

namespace Workflow.Application.Services;

/// <summary>
/// Service for managing budgets and tracking spending
/// </summary>
public class BudgetService
{
    private readonly WorkflowDbContext _db;

    public BudgetService(WorkflowDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a new budget
    /// </summary>
    public async Task<Guid> CreateBudget(Guid userId, string name, decimal amount, DateTime startDate, DateTime endDate, 
        string? description = null, Guid? categoryId = null)
    {
        var budget = new Budget(name, amount, startDate, endDate, description, userId, categoryId);
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();
        return budget.Id;
    }

    /// <summary>
    /// Updates an existing budget
    /// </summary>
    public async Task UpdateBudget(Guid budgetId, string name, decimal amount, DateTime startDate, DateTime endDate, string? description = null)
    {
        var budget = await _db.Budgets.FindAsync(budgetId)
            ?? throw new InvalidOperationException("Budget not found");

        budget.Update(name, amount, startDate, endDate, description);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Activates a budget
    /// </summary>
    public async Task ActivateBudget(Guid budgetId)
    {
        var budget = await _db.Budgets.FindAsync(budgetId)
            ?? throw new InvalidOperationException("Budget not found");

        budget.Activate();
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Deactivates a budget
    /// </summary>
    public async Task DeactivateBudget(Guid budgetId)
    {
        var budget = await _db.Budgets.FindAsync(budgetId)
            ?? throw new InvalidOperationException("Budget not found");

        budget.Deactivate();
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all budgets for a user
    /// </summary>
    public async Task<List<Budget>> GetUserBudgets(Guid userId, bool activeOnly = false)
    {
        var query = _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (activeOnly)
            query = query.Where(b => b.IsActive);

        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Gets budget status with spending information
    /// </summary>
    public async Task<List<BudgetStatus>> GetBudgetStatus(Guid userId)
    {
        var budgets = await _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.IsActive)
            .ToListAsync();

        var budgetStatuses = new List<BudgetStatus>();

        foreach (var budget in budgets)
        {
            // Calculate spent amount based on approved expenses within budget period
            var spentQuery = _db.ExpenseRequests
                .Where(e => e.CreatorId == userId && 
                           e.Status == ExpenseStatus.Approved &&
                           e.ExpenseDate >= budget.StartDate && 
                           e.ExpenseDate <= budget.EndDate);

            // If budget is category-specific, filter by category
            if (budget.CategoryId.HasValue)
            {
                spentQuery = spentQuery.Where(e => e.CategoryId == budget.CategoryId.Value);
            }

            var spentAmount = await spentQuery.SumAsync(e => (decimal?)e.Amount) ?? 0;
            var remainingAmount = budget.Amount - spentAmount;
            var percentageUsed = budget.Amount > 0 ? (double)(spentAmount / budget.Amount * 100) : 0;
            var daysRemaining = (budget.EndDate - DateTime.UtcNow).Days;

            budgetStatuses.Add(new BudgetStatus
            {
                BudgetId = budget.Id,
                BudgetName = budget.Name,
                Description = budget.Description,
                BudgetAmount = budget.Amount,
                SpentAmount = spentAmount,
                RemainingAmount = remainingAmount,
                PercentageUsed = Math.Round(percentageUsed, 2),
                CategoryName = budget.Category?.Name,
                CategoryIcon = budget.Category?.Icon,
                StartDate = budget.StartDate,
                EndDate = budget.EndDate,
                DaysRemaining = Math.Max(0, daysRemaining),
                IsOverBudget = spentAmount > budget.Amount,
                IsActive = budget.IsCurrentlyActive()
            });
        }

        return budgetStatuses.OrderByDescending(b => b.PercentageUsed).ToList();
    }

    /// <summary>
    /// Deletes a budget
    /// </summary>
    public async Task DeleteBudget(Guid budgetId, Guid userId)
    {
        var budget = await _db.Budgets.FindAsync(budgetId)
            ?? throw new InvalidOperationException("Budget not found");

        if (budget.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own budgets");

        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
    }
}
