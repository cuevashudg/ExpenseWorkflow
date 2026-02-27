// TODO: REMOVE BEFORE PROD — Dev-only authentication bypass handler
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Workflow.Domain.Entities;

namespace Workflow.Api.Authorization;

// TODO: REMOVE BEFORE PROD
/// <summary>
/// Development-only authentication handler that bypasses JWT and creates
/// a fake ClaimsPrincipal using a real existing user from the database.
/// Controlled by DevSettings:BypassAuth in appsettings.Development.json.
/// </summary>
public class DevBypassAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    // TODO: REMOVE BEFORE PROD
    public DevBypassAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    // TODO: REMOVE BEFORE PROD
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If bypass is disabled, skip this handler
        if (!_configuration.GetValue<bool>("DevSettings:BypassAuth"))
            return AuthenticateResult.NoResult();

        // If there's a valid Bearer token, forward to JWT handler instead
        var authHeader = Context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var jwtResult = await Context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (jwtResult.Succeeded)
                return jwtResult;
            // If JWT fails, fall through to dev bypass
        }

        // Look up a real user to impersonate
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? user = null;

        // Try configured user ID first
        var devUserId = _configuration["DevSettings:DefaultUserId"];
        if (!string.IsNullOrEmpty(devUserId) && Guid.TryParse(devUserId, out _))
        {
            user = await userManager.FindByIdAsync(devUserId);
        }

        // Fallback: find the first employee user
        // TODO: REMOVE BEFORE PROD
        user ??= await userManager.FindByEmailAsync("employee@example.com");

        if (user == null)
            return AuthenticateResult.Fail("DevBypass: No dev user found in database");

        Logger.LogWarning(
            "TODO: REMOVE BEFORE PROD — DevBypass active, authenticating as {Email} ({Role})",
            user.Email, user.Role);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, "DevBypass");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "DevBypass");

        return AuthenticateResult.Success(ticket);
    }
}
