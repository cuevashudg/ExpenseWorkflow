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
        var userId = GetCurrentUserId();
        var budgets = await _service.GetUserBudgets(userId, activeOnly);
        return Ok(budgets);
    }

    /// <summary>
    /// Gets budget status with spending information
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetBudgetStatus()
    {
        var userId = GetCurrentUserId();
        var status = await _service.GetBudgetStatus(userId);
        return Ok(status);
    }

    /// <summary>
    /// Creates a new budget
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetDto dto)
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
        
        return CreatedAtAction(nameof(GetMyBudgets), new { id = budgetId }, new { id = budgetId });
    }

    /// <summary>
    /// Updates an existing budget
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        await _service.UpdateBudget(id, dto.Name, dto.Amount, dto.StartDate, dto.EndDate, dto.Description);
        return NoContent();
    }

    /// <summary>
    /// Activates a budget
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateBudget(Guid id)
    {
        await _service.ActivateBudget(id);
        return NoContent();
    }

    /// <summary>
    /// Deactivates a budget
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateBudget(Guid id)
    {
        await _service.DeactivateBudget(id);
        return NoContent();
    }

    /// <summary>
    /// Deletes a budget
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = GetCurrentUserId();
        await _service.DeleteBudget(id, userId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
