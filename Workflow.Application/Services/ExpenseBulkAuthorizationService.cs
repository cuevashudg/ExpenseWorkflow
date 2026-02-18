using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Data;

namespace Workflow.Application.Services;

public class ExpenseBulkAuthorizationService
{
    private readonly WorkflowDbContext _db;

    public ExpenseBulkAuthorizationService(WorkflowDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<Guid, bool>> AuthorizeExpensesAsync(ClaimsPrincipal user, IEnumerable<Guid> expenseIds)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = user.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userId, out var userGuid) || !Enum.TryParse<UserRole>(userRole, out var parsedRole))
            return expenseIds.ToDictionary(id => id, id => false);

        // Batch-load all requested expenses and their creators
        var expenses = await _db.ExpenseRequests
            .Where(e => expenseIds.Contains(e.Id))
            .Select(e => new { e.Id, e.CreatorId })
            .ToListAsync();

        // For managers, batch-load all direct report user IDs
        HashSet<Guid> directReportIds = new();
        if (parsedRole == UserRole.Manager)
        {
            directReportIds = (await _db.Users
                .Where(u => u.ManagerId == userGuid)
                .Select(u => u.Id)
                .ToListAsync())
                .ToHashSet();
        }

        // In-memory authorization checks
        var result = new Dictionary<Guid, bool>();
        foreach (var expense in expenses)
        {
            bool authorized =
                parsedRole == UserRole.Admin ||
                (parsedRole == UserRole.Employee && expense.CreatorId == userGuid) ||
                (parsedRole == UserRole.Manager && directReportIds.Contains(expense.CreatorId));

            result[expense.Id] = authorized;
        }
        return result;
    }
}
