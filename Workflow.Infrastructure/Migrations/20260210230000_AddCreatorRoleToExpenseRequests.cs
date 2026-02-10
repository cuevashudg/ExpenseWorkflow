using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.Domain.Enums;

#nullable disable

namespace Workflow.Infrastructure.Migrations
{
    public partial class AddCreatorRoleToExpenseRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatorRole",
                table: "expense_requests",
                type: "integer",
                nullable: false,
                defaultValue: (int)UserRole.Employee);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorRole",
                table: "expense_requests");
        }
    }
}
