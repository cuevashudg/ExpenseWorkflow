using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application.Services;
using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Api.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _service;

    public ExpensesController(ExpenseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Creates a new expense request in draft status
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var expenseId = await _service.CreateExpense(
                userId, 
                dto.Title, 
                dto.Description, 
                dto.Amount, 
                dto.ExpenseDate);
            
            return CreatedAtAction(nameof(GetById), new { id = expenseId }, new { id = expenseId });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var expense = await _service.GetExpenseById(id);
        if (expense == null)
            return NotFound(new { error = "Expense not found" });

        return Ok(expense);
    }

    /// <summary>
    /// Gets all expenses for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyExpenses()
    {
        var userId = GetCurrentUserId();
        var expenses = await _service.GetExpensesByCreator(userId);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets all pending expenses (Manager only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetPending()
    {
        var expenses = await _service.GetPendingExpenses();
        return Ok(expenses);
    }

    /// <summary>
    /// Updates a draft expense
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.UpdateExpense(id, userId, dto.Title, dto.Description, dto.Amount);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Submits an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.SubmitExpense(id, userId);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approves an expense (Manager only)
    /// </summary>
    [Authorize(Policy = "CanApproveExpense")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            await _service.ApproveExpense(id, managerId, userRole);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rejects an expense (Manager only)
    /// </summary>
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectExpenseDto dto)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            await _service.RejectExpense(id, managerId, userRole, dto.Reason);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds an attachment to a draft expense
    /// </summary>
    [HttpPost("{id}/attachments")]
    public async Task<IActionResult> AddAttachment(Guid id, [FromBody] AddAttachmentDto dto)
    {
        try
        {
            await _service.AddAttachment(id, dto.AttachmentUrl);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the audit history for an expense
    /// </summary>
    [HttpGet("{id}/audit-history")]
    public async Task<IActionResult> GetAuditHistory(Guid id)
    {
        try
        {
            var auditLogs = await _service.GetAuditHistory(id);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found in token");
        
        return Guid.Parse(userIdClaim);
    }

    private UserRole GetCurrentUserRole()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(roleClaim))
            throw new UnauthorizedAccessException("User role not found in token");
        
        return Enum.Parse<UserRole>(roleClaim);
    }
}

// DTOs
public record CreateExpenseDto(
    string Title,
    string Description,
    decimal Amount,
    DateTime ExpenseDate
);

public record UpdateExpenseDto(
    string Title,
    string Description,
    decimal Amount
);

public record RejectExpenseDto(
    string Reason
);

public record AddAttachmentDto(
    string AttachmentUrl
);
