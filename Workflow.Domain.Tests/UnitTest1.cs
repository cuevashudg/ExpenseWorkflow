using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Domain.Tests;

public class ExpenseRequestTests
{
    [Fact]
    public void Should_Create_Expense_In_Draft_Status()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var expenseDate = DateTime.UtcNow.AddDays(-5);
        
        // Act
        var expense = new ExpenseRequest(creatorId, "Lunch", "Team lunch", 50m, expenseDate);
        
        // Assert
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Equal(creatorId, expense.CreatorId);
    }
    
    [Fact]
    public void Should_Throw_When_Submitting_Without_Receipt_Over_Threshold()
    {
        // Arrange
        var expense = new ExpenseRequest(Guid.NewGuid(), "Hotel", "Stay", 150m, DateTime.UtcNow.AddDays(-1));
        
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => expense.Submit(expense.CreatorId));
        Assert.Contains("Receipt required", exception.Message);
    }
}
