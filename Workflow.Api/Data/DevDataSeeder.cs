// TODO: REMOVE BEFORE PROD — Dev-only data seeder
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Infrastructure.Data;

namespace Workflow.Api.Data;

// TODO: REMOVE BEFORE PROD
/// <summary>
/// Seeds realistic development data when DevSettings:BypassAuth is true.
/// Creates 2 employees, 1 manager, 1 admin, 10 sample expenses in mixed states,
/// and sample budgets. Uses the same user ID as the DevBypassAuthHandler.
/// </summary>
public static class DevDataSeeder
{
    // TODO: REMOVE BEFORE PROD
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = serviceProvider.GetRequiredService<WorkflowDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<WorkflowDbContext>>();

        logger.LogWarning("TODO: REMOVE BEFORE PROD — DevDataSeeder is running");

        // ----- Seed Users -----
        var employee1 = await EnsureUser(userManager, "employee@example.com", "Employee One", UserRole.Employee, "Employee");
        var employee2 = await EnsureUser(userManager, "employee2@example.com", "Employee Two", UserRole.Employee, "Employee");
        var manager = await EnsureUser(userManager, "manager@example.com", "Test Manager", UserRole.Manager, "Manager");
        var admin = await EnsureUser(userManager, "admin@example.com", "Test Admin", UserRole.Admin, "Admin");

        // Set manager relationships
        if (employee1 != null && manager != null && employee1.ManagerId != manager.Id)
        {
            employee1.ManagerId = manager.Id;
            await userManager.UpdateAsync(employee1);
        }
        if (employee2 != null && manager != null && employee2.ManagerId != manager.Id)
        {
            employee2.ManagerId = manager.Id;
            await userManager.UpdateAsync(employee2);
        }

        // ----- Seed Expenses (only if none exist) -----
        if (employee1 == null || manager == null || admin == null)
        {
            logger.LogWarning("DevDataSeeder: Could not find/create all users, skipping expense seeding");
            return;
        }

        var existingCount = await db.ExpenseRequests.CountAsync();
        if (existingCount > 0)
        {
            logger.LogInformation("DevDataSeeder: {Count} expenses already exist, skipping expense seeding", existingCount);
            return;
        }

        // Category IDs from ExpenseCategoryConfiguration seed data
        var travelCatId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mealsCatId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var officeCatId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var softwareCatId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var trainingCatId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        var now = DateTime.UtcNow;

        // 10 sample expenses in mixed states
        var expenses = new List<ExpenseRequest>
        {
            // Employee 1 - Draft (low amount, no receipt needed)
            CreateExpense(employee1.Id, "Office keyboard", "Mechanical keyboard for desk", 75.00m,
                now.AddDays(-2), officeCatId),

            // Employee 1 - Draft (high amount, will need receipt before submit)
            CreateExpense(employee1.Id, "Conference registration", "Annual dev conference 2026", 450.00m,
                now.AddDays(-5), trainingCatId),

            // Employee 1 - Submitted
            CreateSubmittedExpense(employee1.Id, "Team lunch Friday", "Team building lunch at restaurant", 85.50m,
                now.AddDays(-7), mealsCatId),

            // Employee 1 - Approved
            CreateApprovedExpense(employee1.Id, "Flight to NYC", "Client meeting travel", 320.00m,
                now.AddDays(-15), travelCatId, manager.Id),

            // Employee 1 - Rejected
            CreateRejectedExpense(employee1.Id, "Personal laptop", "New MacBook Pro", 2499.99m,
                now.AddDays(-20), softwareCatId, manager.Id, "Personal equipment is not reimbursable"),

            // Employee 2 - Draft
            CreateExpense(employee2!.Id, "USB-C hub", "Docking station for laptop", 65.00m,
                now.AddDays(-1), officeCatId),

            // Employee 2 - Submitted
            CreateSubmittedExpense(employee2.Id, "Client dinner", "Dinner with client stakeholders", 195.00m,
                now.AddDays(-3), mealsCatId),

            // Employee 2 - Approved
            CreateApprovedExpense(employee2.Id, "IDE license", "JetBrains annual subscription", 149.00m,
                now.AddDays(-30), softwareCatId, manager.Id),

            // Manager - Submitted (needs admin approval)
            CreateSubmittedExpense(manager.Id, "Leadership workshop", "2-day management training", 800.00m,
                now.AddDays(-4), trainingCatId),

            // Manager - Approved by admin
            CreateApprovedExpense(manager.Id, "Team offsite venue", "Q1 team offsite booking", 1200.00m,
                now.AddDays(-25), travelCatId, admin.Id),
        };

        db.ExpenseRequests.AddRange(expenses);

        // Create corresponding audit logs
        foreach (var expense in expenses)
        {
            db.AuditLogs.Add(AuditLog.ForCreation(expense.Id, expense.CreatorId));

            if (expense.Status == ExpenseStatus.Submitted)
            {
                db.AuditLogs.Add(AuditLog.ForSubmission(expense.Id, expense.CreatorId));
            }
            else if (expense.Status == ExpenseStatus.Approved && expense.ProcessedBy.HasValue)
            {
                db.AuditLogs.Add(AuditLog.ForSubmission(expense.Id, expense.CreatorId));
                db.AuditLogs.Add(AuditLog.ForApproval(expense.Id, expense.ProcessedBy.Value));
            }
            else if (expense.Status == ExpenseStatus.Rejected && expense.ProcessedBy.HasValue)
            {
                db.AuditLogs.Add(AuditLog.ForSubmission(expense.Id, expense.CreatorId));
                db.AuditLogs.Add(AuditLog.ForRejection(expense.Id, expense.ProcessedBy.Value, expense.RejectionReason ?? ""));
            }
        }

        // ----- Seed Budgets (only if none exist) -----
        var existingBudgets = await db.Budgets.CountAsync();
        if (existingBudgets == 0)
        {
            var budgets = new List<Budget>
            {
                new Budget("Monthly Travel", 1500.00m,
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc),
                    "Monthly travel budget", employee1.Id, travelCatId),

                new Budget("Q1 Software", 500.00m,
                    new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(now.Year, 3, 31, 23, 59, 59, DateTimeKind.Utc),
                    "Quarterly software subscriptions", employee1.Id, softwareCatId),

                new Budget("Team Meals", 300.00m,
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc),
                    "Monthly team meals budget", employee2.Id, mealsCatId),

                new Budget("Training Annual", 2000.00m,
                    new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(now.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                    "Annual training and education budget", manager.Id, trainingCatId),
            };

            db.Budgets.AddRange(budgets);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("DevDataSeeder: Seeded {ExpenseCount} expenses and sample budgets", expenses.Count);
    }

    // TODO: REMOVE BEFORE PROD
    private static async Task<ApplicationUser?> EnsureUser(
        UserManager<ApplicationUser> userManager, string email, string fullName, UserRole role, string roleName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, roleName);
            return user;
        }

        return null;
    }

    private static ExpenseRequest CreateExpense(
        Guid creatorId, string title, string description, decimal amount, DateTime expenseDate, Guid categoryId)
    {
        return new ExpenseRequest(creatorId, title, description, amount, expenseDate, categoryId);
    }

    private static ExpenseRequest CreateSubmittedExpense(
        Guid creatorId, string title, string description, decimal amount, DateTime expenseDate, Guid categoryId)
    {
        var expense = new ExpenseRequest(creatorId, title, description, amount, expenseDate, categoryId);
        // Add a dummy attachment for expenses over $100 (required by domain rule)
        if (amount > 100)
            expense.AddAttachment("dev-receipt-placeholder.pdf");
        expense.Submit(creatorId);
        return expense;
    }

    private static ExpenseRequest CreateApprovedExpense(
        Guid creatorId, string title, string description, decimal amount, DateTime expenseDate, 
        Guid categoryId, Guid approverId)
    {
        var expense = new ExpenseRequest(creatorId, title, description, amount, expenseDate, categoryId);
        if (amount > 100)
            expense.AddAttachment("dev-receipt-placeholder.pdf");
        expense.Submit(creatorId);
        // Use Admin role to bypass all approval restrictions in domain 
        expense.Approve(approverId, UserRole.Admin);
        return expense;
    }

    private static ExpenseRequest CreateRejectedExpense(
        Guid creatorId, string title, string description, decimal amount, DateTime expenseDate,
        Guid categoryId, Guid rejectorId, string reason)
    {
        var expense = new ExpenseRequest(creatorId, title, description, amount, expenseDate, categoryId);
        if (amount > 100)
            expense.AddAttachment("dev-receipt-placeholder.pdf");
        expense.Submit(creatorId);
        expense.Reject(rejectorId, UserRole.Manager, reason);
        return expense;
    }
}
