namespace Workflow.Domain.Entities;

public class ExpenseCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Icon { get; private set; }
    public string Color { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private ExpenseCategory()
    {
        Name = string.Empty;
        Description = string.Empty;
        Icon = string.Empty;
        Color = string.Empty;
    }

    public ExpenseCategory(string name, string description, string icon, string color)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Icon = icon;
        Color = color;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
