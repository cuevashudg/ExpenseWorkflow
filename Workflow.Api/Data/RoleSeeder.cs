using Microsoft.AspNetCore.Identity;

namespace Workflow.Api.Data;

/// <summary>
/// Seeds default roles into the Identity system.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seeds Employee and Manager roles if they don't exist.
    /// </summary>
    /// <param name="services">Service provider for dependency resolution.</param>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        string[] roles = { "Employee", "Manager", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}
