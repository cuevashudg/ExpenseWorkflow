using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaticCategorySeedFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "expense_requests",
                type: "uuid",
                nullable: true);

            // CreatorName column already exists, skip adding it again

            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budgets_expense_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "expense_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "expense_categories",
                columns: new[] { "Id", "Color", "CreatedAt", "Description", "Icon", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "#3b82f6", new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Business travel expenses including flights, hotels, car rentals", "✈️", true, "Travel" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "#f59e0b", new DateTime(2023, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Client meals, team lunches, and entertainment expenses", "🍽️", true, "Meals & Entertainment" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "#10b981", new DateTime(2023, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Stationery, furniture, and general office equipment", "📎", true, "Office Supplies" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "#8b5cf6", new DateTime(2023, 1, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Software licenses, SaaS subscriptions, cloud services", "💻", true, "Software & Subscriptions" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "#ec4899", new DateTime(2023, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Professional development, courses, certifications, conferences", "📚", true, "Training & Education" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "#6b7280", new DateTime(2023, 1, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Miscellaneous expenses not covered by other categories", "📋", true, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_requests_CategoryId",
                table: "expense_requests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_budgets_CategoryId",
                table: "budgets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_budgets_IsActive_StartDate_EndDate",
                table: "budgets",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_budgets_UserId",
                table: "budgets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_requests_expense_categories_CategoryId",
                table: "expense_requests",
                column: "CategoryId",
                principalTable: "expense_categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_requests_expense_categories_CategoryId",
                table: "expense_requests");

            migrationBuilder.DropTable(
                name: "budgets");

            migrationBuilder.DropTable(
                name: "ExpenseComments");

            migrationBuilder.DropTable(
                name: "expense_categories");

            migrationBuilder.DropIndex(
                name: "IX_expense_requests_CategoryId",
                table: "expense_requests");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "expense_requests");

            // CreatorName column already exists, skip dropping it
        }
    }
}
