using Workflow.Domain.Enums;

namespace Workflow.Application.Models;

public class ExpenseQuery
{
    public string? Search { get; init; }
    public ExpenseStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? SortBy { get; init; }
    public string? SortDir { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
}
