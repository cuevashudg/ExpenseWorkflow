using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Data;

namespace Workflow.Application.Services;

/// <summary>
/// Application service for managing expense request operations.
/// Coordinates between API layer and Domain/Infrastructure layers.
/// </summary>
public class ExpenseService
{
    private readonly WorkflowDbContext _db;

    public ExpenseService(WorkflowDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a new expense request in draft status.
    /// </summary>
    public async Task<Guid> CreateExpense(Guid userId, string title, string description, decimal amount, DateTime expenseDate)
    {
        var expense = new ExpenseRequest(userId, title, description, amount, expenseDate);
        _db.ExpenseRequests.Add(expense);
        await _db.SaveChangesAsync();
        return expense.Id;
    }

    /// <summary>
    /// Updates a draft expense request.
    /// </summary>
    public async Task UpdateExpense(Guid expenseId, Guid userId, string title, string description, decimal amount)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.Update(userId, title, description, amount);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Submits an expense request for approval.
    /// </summary>
    public async Task SubmitExpense(Guid expenseId, Guid userId)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.Submit(userId);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Approves an expense request.
    /// </summary>
    public async Task ApproveExpense(Guid expenseId, Guid managerId, UserRole userRole)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.Approve(managerId, userRole);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rejects an expense request with a reason.
    /// </summary>
    public async Task RejectExpense(Guid expenseId, Guid managerId, UserRole userRole, string reason)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.Reject(managerId, userRole, reason);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gets an expense request by ID.
    /// </summary>
    public async Task<ExpenseRequest?> GetExpenseById(Guid expenseId)
    {
        return await _db.ExpenseRequests.FindAsync(expenseId);
    }

    /// <summary>
    /// Gets all expense requests for a specific user.
    /// </summary>
    public async Task<List<ExpenseRequest>> GetExpensesByCreator(Guid userId)
    {
        return await _db.ExpenseRequests
            .Where(e => e.CreatorId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all pending (submitted) expense requests for manager review.
    /// </summary>
    public async Task<List<ExpenseRequest>> GetPendingExpenses()
    {
        return await _db.ExpenseRequests
            .Where(e => e.Status == ExpenseStatus.Submitted)
            .OrderBy(e => e.SubmittedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Adds an attachment to a draft expense.
    /// </summary>
    public async Task AddAttachment(Guid expenseId, string attachmentUrl)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.AddAttachment(attachmentUrl);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Removes an attachment from a draft expense.
    /// </summary>
    public async Task RemoveAttachment(Guid expenseId, string attachmentUrl)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.RemoveAttachment(attachmentUrl);
        await _db.SaveChangesAsync();
    }
}
