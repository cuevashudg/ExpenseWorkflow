using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application.Models;
using Workflow.Application.Services;

namespace Workflow.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly BudgetService _service;

    public BudgetsController(BudgetService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all budgets for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyBudgets([FromQuery] bool activeOnly = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var budgets = await _service.GetUserBudgets(userId, activeOnly);
            return Ok(ApiResponse<object>.Ok(budgets));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Gets budget status with spending information
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetBudgetStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _service.GetBudgetStatus(userId);
            return Ok(ApiResponse<object>.Ok(status));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Creates a new budget
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var budgetId = await _service.CreateBudget(
                userId, 
                dto.Name, 
                dto.Amount, 
                dto.StartDate, 
                dto.EndDate, 
                dto.Description, 
                dto.CategoryId);
            
            return CreatedAtAction(nameof(GetMyBudgets), new { id = budgetId }, 
                ApiResponse<object>.Ok(new { id = budgetId }));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing budget
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        try
        {
            await _service.UpdateBudget(id, dto.Name, dto.Amount, dto.StartDate, dto.EndDate, dto.Description);
            return Ok(ApiResponse.Ok());
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Activates a budget
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateBudget(Guid id)
    {
        try
        {
            await _service.ActivateBudget(id);
            return Ok(ApiResponse.Ok());
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Deactivates a budget
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateBudget(Guid id)
    {
        try
        {
            await _service.DeactivateBudget(id);
            return Ok(ApiResponse.Ok());
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Deletes a budget
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.DeleteBudget(id, userId);
            return Ok(ApiResponse.Ok());
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
