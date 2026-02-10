using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Workflow.Application.Models;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;
using Workflow.Infrastructure.Data;

namespace Workflow.Application.Services;

/// <summary>
/// Application service for managing expense request operations.
/// Coordinates between API layer and Domain/Infrastructure layers.
/// </summary>
public class ExpenseService
{
    private readonly WorkflowDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExpenseService(WorkflowDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>
    /// Creates a new expense request in draft status.
    /// </summary>
    public async Task<Guid> CreateExpense(Guid userId, string title, string description, decimal amount, DateTime expenseDate, Guid? categoryId = null)
    {
        var expense = new ExpenseRequest(userId, title, description, amount, expenseDate, categoryId);
        _db.ExpenseRequests.Add(expense);
        
        // Create audit log
        var auditLog = AuditLog.ForCreation(expense.Id, userId);
        _db.AuditLogs.Add(auditLog);
        
        await _db.SaveChangesAsync();
        return expense.Id;
    }

    /// <summary>
    /// Updates a draft expense request.
    /// </summary>
    public async Task UpdateExpense(Guid expenseId, Guid userId, string title, string description, decimal amount, Guid? categoryId = null)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        expense.Update(userId, title, description, amount, categoryId);
        
        // Create audit log
        var changes = $"Updated: Title='{title}', Description='{description}', Amount=${amount}";
        var auditLog = AuditLog.ForUpdate(expenseId, userId, changes);
        _db.AuditLogs.Add(auditLog);
        
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
        
        // Create audit log
        var auditLog = AuditLog.ForSubmission(expenseId, userId);
        _db.AuditLogs.Add(auditLog);
        
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Approves an expense request.
    /// </summary>
    public async Task ApproveExpense(Guid expenseId, Guid managerId, UserRole userRole)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        // Set creator role for domain logic
        var creatorRole = await GetUserRole(expense.CreatorId);
        expense.GetType().GetProperty("CreatorRole")?.SetValue(expense, creatorRole);

        expense.Approve(managerId, userRole);

        // Create audit log
        var auditLog = AuditLog.ForApproval(expenseId, managerId);
        _db.AuditLogs.Add(auditLog);

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
        
        // Create audit log
        var auditLog = AuditLog.ForRejection(expenseId, managerId, reason);
        _db.AuditLogs.Add(auditLog);
        
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gets an expense request by ID with creator name enriched.
    /// </summary>
    public async Task<ExpenseRequest?> GetExpenseById(Guid expenseId)
    {
        var expense = await _db.ExpenseRequests
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == expenseId);
        
        if (expense != null)
        {
            // Enrich with creator name
            var user = await _userManager.FindByIdAsync(expense.CreatorId.ToString());
            if (user != null)
            {
                expense.CreatorName = user.FullName ?? user.Email ?? "Unknown";
            }
        }
        
        return expense;
    }

    /// <summary>
    /// Gets all expense requests for a specific user with pagination.
    /// </summary>
    public async Task<PagedResult<ExpenseRequest>> GetExpensesByCreator(Guid userId, ExpenseQuery query)
    {
        var dbQuery = _db.ExpenseRequests
            .Include(e => e.Category)
            .Where(e => e.CreatorId == userId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            dbQuery = dbQuery.Where(e => e.Title.ToLower().Contains(search) || e.Description.ToLower().Contains(search));
        }

        if (query.Status.HasValue)
            dbQuery = dbQuery.Where(e => e.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            dbQuery = dbQuery.Where(e => e.ExpenseDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            dbQuery = dbQuery.Where(e => e.ExpenseDate <= query.ToDate.Value);

        if (query.MinAmount.HasValue)
            dbQuery = dbQuery.Where(e => e.Amount >= query.MinAmount.Value);

        if (query.MaxAmount.HasValue)
            dbQuery = dbQuery.Where(e => e.Amount <= query.MaxAmount.Value);

        // Apply sorting
        dbQuery = (query.SortBy?.ToLower(), query.SortDir?.ToLower()) switch
        {
            ("amount", "asc") => dbQuery.OrderBy(e => e.Amount),
            ("amount", "desc") => dbQuery.OrderByDescending(e => e.Amount),
            ("expensedate", "asc") => dbQuery.OrderBy(e => e.ExpenseDate),
            ("expensedate", "desc") => dbQuery.OrderByDescending(e => e.ExpenseDate),
            ("submittedat", "asc") => dbQuery.OrderBy(e => e.SubmittedAt),
            ("submittedat", "desc") => dbQuery.OrderByDescending(e => e.SubmittedAt),
            _ => dbQuery.OrderByDescending(e => e.CreatedAt)
        };

        var totalCount = await dbQuery.CountAsync();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ExpenseRequest>(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// Gets all pending (submitted) expense requests for manager review with pagination.
    /// </summary>
    public async Task<PagedResult<ExpenseRequest>> GetPendingExpenses(ExpenseQuery query)
    {
        var dbQuery = _db.ExpenseRequests
            .Include(e => e.Category)
            .Where(e => e.Status == ExpenseStatus.Submitted);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            dbQuery = dbQuery.Where(e => e.Title.ToLower().Contains(search) || e.Description.ToLower().Contains(search));
        }

        if (query.FromDate.HasValue)
            dbQuery = dbQuery.Where(e => e.ExpenseDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            dbQuery = dbQuery.Where(e => e.ExpenseDate <= query.ToDate.Value);

        if (query.MinAmount.HasValue)
            dbQuery = dbQuery.Where(e => e.Amount >= query.MinAmount.Value);

        if (query.MaxAmount.HasValue)
            dbQuery = dbQuery.Where(e => e.Amount <= query.MaxAmount.Value);

        // Apply sorting
        dbQuery = (query.SortBy?.ToLower(), query.SortDir?.ToLower()) switch
        {
            ("amount", "asc") => dbQuery.OrderBy(e => e.Amount),
            ("amount", "desc") => dbQuery.OrderByDescending(e => e.Amount),
            ("expensedate", "asc") => dbQuery.OrderBy(e => e.ExpenseDate),
            ("expensedate", "desc") => dbQuery.OrderByDescending(e => e.ExpenseDate),
            _ => dbQuery.OrderBy(e => e.SubmittedAt)
        };

        var totalCount = await dbQuery.CountAsync();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Enrich with creator names
        foreach (var expense in items)
        {
            var user = await _userManager.FindByIdAsync(expense.CreatorId.ToString());
            if (user != null)
            {
                expense.CreatorName = user.FullName ?? user.Email ?? "Unknown";
            }
        }

        return new PagedResult<ExpenseRequest>(items, totalCount, page, pageSize);
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
    /// Gets audit history for an expense.
    /// </summary>
    public async Task<List<AuditLog>> GetAuditHistory(Guid expenseId)
    {
        return await _db.AuditLogs
            .Where(a => a.ExpenseRequestId == expenseId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Deletes a draft expense (creator only).
    /// </summary>
    public async Task DeleteExpense(Guid expenseId, Guid userId)
    {
        var expense = await _db.ExpenseRequests.FindAsync(expenseId)
            ?? throw new InvalidOperationException("Expense not found");

        if (expense.Status != ExpenseStatus.Draft)
            throw new DomainException("Only draft expenses can be deleted.");

        if (expense.CreatorId != userId)
            throw new DomainException("Only the creator can delete this expense.");

        _db.ExpenseRequests.Remove(expense);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the role of a user by ID
    /// </summary>
    public async Task<UserRole> GetUserRole(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.Role ?? UserRole.Employee;
    }

    /// <summary>
    /// Gets comments for an expense (if implemented).
    /// </summary>
    public async Task<List<ExpenseComment>> GetComments(Guid expenseId)
    {
        return await _db.ExpenseComments
            .Where(c => c.ExpenseRequestId == expenseId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a comment to an expense (Manager/Admin only).
    /// </summary>
    public async Task<ExpenseComment> AddComment(Guid expenseId, Guid userId, string text)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found");

        var comment = new ExpenseComment
        {
            ExpenseRequestId = expenseId,
            UserId = userId,
            UserName = user.FullName ?? user.Email ?? "Unknown",
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

        _db.ExpenseComments.Add(comment);
        await _db.SaveChangesAsync();

        return comment;
    }

    /// <summary>
    /// Gets analytics data for the user's expenses
    /// </summary>
    public async Task<ExpenseAnalytics> GetUserAnalytics(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.ExpenseRequests.Where(e => e.CreatorId == userId);

        if (startDate.HasValue)
            query = query.Where(e => e.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.CreatedAt <= endDate.Value);

        var expenses = await query.Include(e => e.Category).ToListAsync();

        var analytics = new ExpenseAnalytics
        {
            TotalExpenses = expenses.Sum(e => e.Amount),
            ApprovedAmount = expenses.Where(e => e.Status == ExpenseStatus.Approved).Sum(e => e.Amount),
            PendingAmount = expenses.Where(e => e.Status == ExpenseStatus.Submitted).Sum(e => e.Amount),
            TotalCount = expenses.Count,
            ApprovedCount = expenses.Count(e => e.Status == ExpenseStatus.Approved),
            PendingCount = expenses.Count(e => e.Status == ExpenseStatus.Submitted),
            RejectedCount = expenses.Count(e => e.Status == ExpenseStatus.Rejected),
            AverageExpense = expenses.Any() ? expenses.Average(e => e.Amount) : 0
        };

        // Category breakdown
        analytics.CategoryBreakdown = expenses
            .GroupBy(e => new { e.CategoryId, e.Category })
            .Select(g => new CategorySpending
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Category?.Name ?? "Uncategorized",
                CategoryIcon = g.Key.Category?.Icon ?? "📋",
                CategoryColor = g.Key.Category?.Color ?? "#6b7280",
                TotalAmount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Monthly trends (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlyExpenses = await _db.ExpenseRequests
            .Where(e => e.CreatorId == userId && e.CreatedAt >= sixMonthsAgo)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new MonthlyTrend
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        foreach (var trend in monthlyExpenses)
        {
            trend.MonthName = new DateTime(trend.Year, trend.Month, 1).ToString("MMM yyyy");
        }

        analytics.MonthlyTrends = monthlyExpenses.OrderBy(m => m.Year).ThenBy(m => m.Month).ToList();

        return analytics;
    }

    /// <summary>
    /// Gets all active expense categories
    /// </summary>
    public async Task<List<ExpenseCategory>> GetCategories()
    {
        return await _db.ExpenseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets status distribution analytics
    /// </summary>
    public async Task<List<StatusDistribution>> GetStatusDistribution(Guid? userId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.ExpenseRequests.AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.CreatorId == userId.Value);

        if (startDate.HasValue)
            query = query.Where(e => e.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.CreatedAt <= endDate.Value);

        var total = await query.CountAsync();
        if (total == 0) return new List<StatusDistribution>();

        var distribution = await query
            .GroupBy(e => e.Status)
            .Select(g => new StatusDistribution
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(e => e.Amount),
                Percentage = 0
            })
            .ToListAsync();

        foreach (var item in distribution)
        {
            item.Percentage = Math.Round((double)item.Count / total * 100, 2);
        }

        return distribution.OrderByDescending(d => d.Count).ToList();
    }

    /// <summary>
    /// Gets approval rate analytics over time (monthly)
    /// </summary>
    public async Task<List<ApprovalRateData>> GetApprovalRates(Guid? userId = null, int monthsBack = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-monthsBack);
        var query = _db.ExpenseRequests
            .Where(e => e.SubmittedAt != null && e.SubmittedAt >= startDate);

        if (userId.HasValue)
            query = query.Where(e => e.CreatorId == userId.Value);

        var monthlyData = await query
            .GroupBy(e => new { 
                Year = e.SubmittedAt!.Value.Year, 
                Month = e.SubmittedAt!.Value.Month 
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalSubmitted = g.Count(),
                Approved = g.Count(e => e.Status == ExpenseStatus.Approved),
                Rejected = g.Count(e => e.Status == ExpenseStatus.Rejected)
            })
            .ToListAsync();

        var result = monthlyData.Select(m => new ApprovalRateData
        {
            Period = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"),
            TotalSubmitted = m.TotalSubmitted,
            Approved = m.Approved,
            Rejected = m.Rejected,
            ApprovalRate = m.TotalSubmitted > 0 ? Math.Round((double)m.Approved / m.TotalSubmitted * 100, 2) : 0,
            RejectionRate = m.TotalSubmitted > 0 ? Math.Round((double)m.Rejected / m.TotalSubmitted * 100, 2) : 0
        })
        .OrderBy(r => r.Period)
        .ToList();

        return result;
    }
}
