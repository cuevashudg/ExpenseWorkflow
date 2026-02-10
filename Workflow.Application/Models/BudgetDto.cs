namespace Workflow.Application.Models;

/// <summary>
/// DTO for creating a new budget
/// </summary>
public class CreateBudgetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? CategoryId { get; set; }
}

/// <summary>
/// DTO for updating an existing budget
/// </summary>
public class UpdateBudgetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
