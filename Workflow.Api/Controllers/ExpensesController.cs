using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workflow.Application.Models;
using Workflow.Application.Services;
using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Api.Controllers
{
    [ApiController]
    [Route("api/expenses")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly ExpenseService _service;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ExpensesController(ExpenseService service, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _service = service;
            _environment = environment;
            _configuration = configuration;
        }

        // --- Bulk Approve Endpoint ---
        /// <summary>
        /// Bulk approve expenses with efficient batch authorization
        /// </summary>
        [HttpPost("bulk-approve")]
        public async Task<IActionResult> BulkApprove([FromBody] List<Guid> expenseIds, [FromServices] ExpenseBulkAuthorizationService authService)
        {
            var authResults = await authService.AuthorizeExpensesAsync(User, expenseIds);
            var unauthorized = authResults.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
            if (unauthorized.Any())
                return Forbid();

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var approved = new List<Guid>();
            var failed = new List<object>();
            foreach (var id in authResults.Where(kv => kv.Value).Select(kv => kv.Key))
            {
                try
                {
                    await _service.ApproveExpense(id, userId, userRole);
                    approved.Add(id);
                }
                catch (Exception ex)
                {
                    failed.Add(new { id, error = ex.Message });
                }
            }
            return Ok(ApiResponse<object>.Ok(new { approved, failed }));
        }

        // --- All Other Controller Methods ---
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
                
                return CreatedAtAction(nameof(GetById), new { id = expenseId }, ApiResponse<object>.Ok(new { id = expenseId }));
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
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
                return NotFound(ApiResponse.Fail("Expense not found"));

            return Ok(ApiResponse<object>.Ok(expense));
        }

        /// <summary>
        /// Gets all expenses for the current user (filtered, sorted, paged)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyExpenses([FromQuery] ExpenseQuery query)
        {
            var userId = GetCurrentUserId();
            var expenses = await _service.GetExpensesByCreator(userId, query);
            return Ok(ApiResponse<object>.Ok(expenses));
        }

        /// <summary>
        /// Gets all pending expenses (Manager only, filtered, sorted, paged)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> GetPending([FromQuery] ExpenseQuery query)
        {
            var expenses = await _service.GetPendingExpenses(query);
            return Ok(ApiResponse<object>.Ok(expenses));
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
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
                
                // Business logic (role check + approval) now fully in service layer
                await _service.ApproveExpense(id, managerId, userRole);
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
        return BadRequest(ApiResponse.Fail("Receipt file is required."));

    const long maxSizeBytes = 5 * 1024 * 1024;
    if (receipt.Length > maxSizeBytes)
        return BadRequest(ApiResponse.Fail("Receipt file must be 5MB or smaller."));

    var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf"
    };

    var ext = Path.GetExtension(receipt.FileName).ToLowerInvariant();
    if (!allowedTypes.Contains(ext))
        return BadRequest(ApiResponse.Fail("Invalid file type. Only images and PDFs are allowed."));

    // Build the uploads directory and create it if it doesn't exist
    var uploadsDir = Path.Combine(
        _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        "uploads", "receipts");

    if (!Directory.Exists(uploadsDir))
        Directory.CreateDirectory(uploadsDir);

    // Generate a unique filename to avoid collisions
    var uniqueName = $"{Guid.NewGuid()}{ext}";
    var filePath = Path.Combine(uploadsDir, uniqueName);

    try
    {
        // Actually write the file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await receipt.CopyToAsync(stream);
        }

        // Save only the filename, not the full path
        await _service.AddAttachment(id, uniqueName);
        return Ok(ApiResponse.Ok());
    }
    catch (Exception ex)
    {
        return StatusCode(500, ApiResponse.Fail("Failed to save file."));
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
                return Ok(ApiResponse.Ok());
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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
                return Ok(ApiResponse<object>.Ok(auditLogs));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Downloads an attachment for an expense with resource-based authorization
        /// </summary>
        [Authorize]
        [HttpGet("{expenseId}/attachments/{fileName}")]
        public async Task<IActionResult> DownloadAttachment(Guid expenseId, string fileName, [FromServices] IAuthorizationService authorizationService, [FromServices] Workflow.Infrastructure.Data.WorkflowDbContext db)
        {
            // Validate filename to prevent directory traversal attacks
            if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return BadRequest(ApiResponse.Fail("Invalid filename."));
            }

            // TODO: REMOVE BEFORE PROD — Dev file access bypass
            if (_configuration.GetValue<bool>("DevSettings:BypassAuth"))
            {
                var devUploadsPath = _configuration["DevSettings:UploadsPath"]
                    ?? Path.Combine(
                        _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                        "uploads", "receipts");

                var devFilePath = Path.Combine(devUploadsPath, fileName);
                var devFullPath = Path.GetFullPath(devFilePath);
                var devFullUploads = Path.GetFullPath(devUploadsPath);
                if (!devFullPath.StartsWith(devFullUploads, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (!System.IO.File.Exists(devFilePath))
                    return NotFound(ApiResponse.Fail("Attachment not found."));

                var devStream = new FileStream(devFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                return File(devStream, GetContentType(fileName), Path.GetFileName(devFilePath));
            }

            var expense = await db.ExpenseRequests.FindAsync(expenseId);
            if (expense == null)
                return NotFound(ApiResponse.Fail("Expense not found."));

            // Resource-based authorization
            var authResult = await authorizationService.AuthorizeAsync(User, expense, "ExpenseAccess");
            if (!authResult.Succeeded)
                return Forbid();

            var uploadsPath = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "receipts");
            var filePath = Path.Combine(uploadsPath, fileName);

            // Security: Verify file is within receipts directory (prevent path traversal)
            var fullPath = Path.GetFullPath(filePath);
            var fullUploadsPath = Path.GetFullPath(uploadsPath);
            if (!fullPath.StartsWith(fullUploadsPath, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(ApiResponse.Fail("Attachment not found."));
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var contentType = GetContentType(fileName);
            return File(stream, contentType, System.IO.Path.GetFileName(filePath));
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
                return Ok(ApiResponse<object>.Ok(comments));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
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
                return CreatedAtAction(nameof(GetComments), new { id = id }, ApiResponse<object>.Ok(comment));
            }
            catch (DomainException ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse.Fail(ex.Message));
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

        private string GetContentType(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        // --- DTOs ---
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
    }
}
