using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workflow.Domain.Entities;

namespace Workflow.Infrastructure.Data.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("expense_categories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Icon)
            .HasMaxLength(50);

        builder.Property(e => e.Color)
            .HasMaxLength(20);

        // Seed default categories
        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Travel",
                Description = "Business travel expenses including flights, hotels, car rentals",
                Icon = "✈️",
                Color = "#3b82f6",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Meals & Entertainment",
                Description = "Client meals, team lunches, and entertainment expenses",
                Icon = "🍽️",
                Color = "#f59e0b",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Office Supplies",
                Description = "Stationery, furniture, and general office equipment",
                Icon = "📎",
                Color = "#10b981",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Software & Subscriptions",
                Description = "Software licenses, SaaS subscriptions, cloud services",
                Icon = "💻",
                Color = "#8b5cf6",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Training & Education",
                Description = "Professional development, courses, certifications, conferences",
                Icon = "📚",
                Color = "#ec4899",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "Other",
                Description = "Miscellaneous expenses not covered by other categories",
                Icon = "📋",
                Color = "#6b7280",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
