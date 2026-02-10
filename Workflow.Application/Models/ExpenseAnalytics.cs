namespace Workflow.Application.Models;

/// <summary>
/// Analytics data for expense dashboard
/// </summary>
public class ExpenseAnalytics
{
    public decimal TotalExpenses { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int TotalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal AverageExpense { get; set; }
    public List<CategorySpending> CategoryBreakdown { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
}

public class CategorySpending
{
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

public class MonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Status distribution analytics
/// </summary>
public class StatusDistribution
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Approval rate analytics over time
/// </summary>
public class ApprovalRateData
{
    public string Period { get; set; } = string.Empty;
    public int TotalSubmitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public double ApprovalRate { get; set; }
    public double RejectionRate { get; set; }
}

/// <summary>
/// Budget tracking data
/// </summary>
public class BudgetStatus
{
    public Guid BudgetId { get; set; }
    public string BudgetName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public double PercentageUsed { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryIcon { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsOverBudget { get; set; }
    public bool IsActive { get; set; }
}
