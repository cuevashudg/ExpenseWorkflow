using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application.Models;
using Workflow.Application.Services;

namespace Workflow.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ExpenseService _service;

    public AnalyticsController(ExpenseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets analytics data for the current user's expenses
    /// </summary>
    [HttpGet("my-expenses")]
    public async Task<IActionResult> GetMyExpensesAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var analytics = await _service.GetUserAnalytics(userId, startDate, endDate);
            return Ok(ApiResponse<object>.Ok(analytics));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all active expense categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _service.GetCategories();
            return Ok(ApiResponse<object>.Ok(categories));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets status distribution for expenses
    /// </summary>
    [HttpGet("status-distribution")]
    public async Task<IActionResult> GetStatusDistribution(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var distribution = await _service.GetStatusDistribution(userId, startDate, endDate);
            return Ok(ApiResponse<object>.Ok(distribution));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets approval rate trends over time
    /// </summary>
    [HttpGet("approval-rates")]
    public async Task<IActionResult> GetApprovalRates([FromQuery] int monthsBack = 6)
    {
        try
        {
            var userId = GetCurrentUserId();
            var rates = await _service.GetApprovalRates(userId, monthsBack);
            return Ok(ApiResponse<object>.Ok(rates));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets all analytics for manager view (all users)
    /// </summary>
    [HttpGet("manager/status-distribution")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetManagerStatusDistribution(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var distribution = await _service.GetStatusDistribution(null, startDate, endDate);
            return Ok(ApiResponse<object>.Ok(distribution));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets approval rate trends for all users (Manager/Admin only)
    /// </summary>
    [HttpGet("manager/approval-rates")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetManagerApprovalRates([FromQuery] int monthsBack = 6)
    {
        try
        {
            var rates = await _service.GetApprovalRates(null, monthsBack);
            return Ok(ApiResponse<object>.Ok(rates));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(claim);
    }
}
