using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application.Models;
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
    private readonly IWebHostEnvironment _environment;

    public ExpensesController(ExpenseService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
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
                dto.ExpenseDate,
                dto.CategoryId);
            
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
    /// Gets all expenses for the current user (filtered, sorted, paged)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyExpenses([FromQuery] ExpenseQuery query)
    {
        var userId = GetCurrentUserId();
        var expenses = await _service.GetExpensesByCreator(userId, query);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets all pending expenses (Manager only, filtered, sorted, paged)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetPending([FromQuery] ExpenseQuery query)
    {
        var expenses = await _service.GetPendingExpenses(query);
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
            await _service.UpdateExpense(id, userId, dto.Title, dto.Description, dto.Amount, dto.CategoryId);
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
    /// Approves an expense (Manager only for employee expenses, Admin only for manager expenses)
    /// </summary>
    [Authorize(Roles = "Manager,Admin")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var managerId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            // Get the expense to check creator's role
            var expense = await _service.GetExpenseById(id);
            if (expense == null)
                return NotFound(new { error = "Expense not found" });
            
            // Get creator's role
            var creatorRole = await _service.GetUserRole(expense.CreatorId);
            
            // Authorization logic:
            // - Managers can approve Employee expenses only
            // - Admins can approve any expense (both Employee and Manager)
            if (userRole == UserRole.Manager && creatorRole == UserRole.Manager)
            {
                return Forbid("Managers can only approve employee expenses. Contact an administrator to approve manager expenses.");
            }
            
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
    /// Uploads a receipt for a draft expense
    /// </summary>
    [HttpPost("{id}/receipt")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadReceipt(Guid id, IFormFile receipt)
    {
        if (receipt == null || receipt.Length == 0)
        {
            return BadRequest(new { error = "Receipt file is required." });
        }

        const long maxSizeBytes = 5 * 1024 * 1024;
        if (receipt.Length > maxSizeBytes)
        {
            return BadRequest(new { error = "Receipt file must be 5MB or smaller." });
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "application/pdf"
        };

        if (!allowedTypes.Contains(receipt.ContentType))
        {
            return BadRequest(new { error = "Only JPG, PNG, or PDF files are allowed." });
        }

        var uploadsRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var receiptsPath = Path.Combine(uploadsRoot, "uploads", "receipts");
        Directory.CreateDirectory(receiptsPath);

        var fileExtension = Path.GetExtension(receipt.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(receiptsPath, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await receipt.CopyToAsync(stream);
        }

        var fileUrl = $"/uploads/receipts/{fileName}";

        try
        {
            await _service.AddAttachment(id, fileUrl);
            return Ok(new { url = fileUrl });
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
    /// Deletes a draft expense (creator only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _service.DeleteExpense(id, userId);
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

    /// <summary>
    /// Downloads an attachment for an expense with authorization checks
    /// </summary>
    [HttpGet("attachments/{filename}")]
    [Authorize]
    public async Task<IActionResult> DownloadAttachment(string filename)
    {
        try
        {
            // Validate filename to prevent directory traversal attacks
            if (string.IsNullOrEmpty(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\"))
            {
                return BadRequest(new { error = "Invalid filename." });
            }

            var uploadsPath = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "receipts");
            var filePath = Path.Combine(uploadsPath, filename);

            // Security: Verify file is within receipts directory (prevent path traversal)
            var fullPath = Path.GetFullPath(filePath);
            var fullUploadsPath = Path.GetFullPath(uploadsPath);

            if (!fullPath.StartsWith(fullUploadsPath, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Attachment not found." });
            }

            var stream = System.IO.File.OpenRead(filePath);
            var contentType = GetContentType(filename);

            return File(stream, contentType, System.IO.Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets comments for an expense request
    /// </summary>
    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(Guid id)
    {
        try
        {
            var comments = await _service.GetComments(id);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a comment to an expense (Manager/Admin only)
    /// </summary>
    [HttpPost("{id}/comments")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var comment = await _service.AddComment(id, userId, dto.Text);
            return CreatedAtAction(nameof(GetComments), new { id = id }, comment);
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
    /// Downloads a receipt attachment for an expense
    /// </summary>
    [HttpGet("{id}/attachments/download")]
    public IActionResult DownloadReceipt(Guid id, [FromQuery] string fileName)
    {
        var uploadsRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var receiptsPath = Path.Combine(uploadsRoot, "uploads", "receipts");
        var filePath = Path.Combine(receiptsPath, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var contentType = "application/octet-stream";
        return PhysicalFile(filePath, contentType, fileName);
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

    private string GetContentType(string filename)
    {
        var ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}

// DTOs
public record CreateExpenseDto(
    string Title,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    Guid? CategoryId
);

public record UpdateExpenseDto(
    string Title,
    string Description,
    decimal Amount,
    Guid? CategoryId
);

public record RejectExpenseDto(
    string Reason
);

public record AddAttachmentDto(
    string AttachmentUrl
);

public record AddCommentDto(
    string Text
);
