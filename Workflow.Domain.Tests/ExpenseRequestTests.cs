using Workflow.Domain.Entities;
using Workflow.Domain.Enums;
using Workflow.Domain.Exceptions;

namespace Workflow.Domain.Tests;

public class ExpenseRequestTests
{
    /// <summary>
    /// Test: Verifies that creating a new expense request initializes it in Draft status.
    /// Business Rule: All new expense requests must start as drafts.
    /// </summary>
    
    [Fact]
    public void Should_Create_Expense_In_Draft_Status()
    {
        // Arrange - Setup test data and prerequisites
        // Create a unique user ID to represent the person creating the expense
        var creatorId = Guid.NewGuid();
        var expenseDate = DateTime.UtcNow.AddDays(-1);
        
        // Act - Execute the behavior we want to test
        // Create a new expense request using the domain constructor
        // Parameters: creatorId, title, description, amount (50m = 50 decimal), expenseDate
        var expense = new ExpenseRequest(creatorId, "Lunch", "Team lunch", 50m, expenseDate);
        
        // Assert - Verify the results match our expectations
        // Verify the expense was created in Draft status (business rule)
        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        // Verify the creator ID was correctly assigned
        Assert.Equal(creatorId, expense.CreatorId);
    }
    
    /// <summary>
    /// Test: Verifies that submitting an expense over $100 without a receipt throws an exception.
    /// Business Rule: Expenses over the threshold require receipt attachments before submission.
    /// </summary>
    [Fact] // Marks this method as a test
    public void Should_Throw_When_Submitting_Without_Receipt_Over_Threshold()
    {
        // Arrange - Setup test data
        // Create an expense with amount of 150 (over the $100 threshold)
        // This expense has NO attachments added, which should violate the business rule
        var expenseDate = DateTime.UtcNow.AddDays(-1);
        var expense = new ExpenseRequest(Guid.NewGuid(), "Hotel", "Stay", 150m, expenseDate);
        
        // Act & Assert - Execute and verify exception in one step
        // Assert.Throws<T> verifies that the lambda expression throws the specified exception type
        // Lambda: () => expense.Submit(expense.CreatorId) attempts to submit without receipt
        var exception = Assert.Throws<DomainException>(() => expense.Submit(expense.CreatorId));
        // Verify the exception message contains the expected text about receipt requirement
        Assert.Contains("receipt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
