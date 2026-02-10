using Microsoft.AspNetCore.Identity;
using Workflow.Domain.Entities;
using Workflow.Domain.Enums;

namespace Workflow.Api.Data;

/// <summary>
/// Seeds test user accounts for development.
/// </summary>
public static class UserSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Define test users
        var testUsers = new[]
        {
            new { Email = "employee@example.com", FullName = "Test Employee", Role = UserRole.Employee, RoleName = "Employee" },
            new { Email = "manager@example.com", FullName = "Test Manager", Role = UserRole.Manager, RoleName = "Manager" },
            new { Email = "admin@example.com", FullName = "Test Admin", Role = UserRole.Admin, RoleName = "Admin" }
        };

        foreach (var testUser in testUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(testUser.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = testUser.Email,
                    Email = testUser.Email,
                    EmailConfirmed = true,
                    FullName = testUser.FullName,
                    Role = testUser.Role,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, testUser.RoleName);
                    Console.WriteLine($"✓ Created test user: {testUser.Email} (Password123!)");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create {testUser.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
