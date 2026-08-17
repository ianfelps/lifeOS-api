using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BudgetEffectiveFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_category_budgets_category_id",
                table: "category_budgets");

            migrationBuilder.AddColumn<DateOnly>(
                name: "effective_from",
                table: "category_budgets",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_category_budgets_category_id",
                table: "category_budgets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_budgets_category_id_effective_from",
                table: "category_budgets",
                columns: new[] { "category_id", "effective_from" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_category_budgets_category_id",
                table: "category_budgets");

            migrationBuilder.DropIndex(
                name: "IX_category_budgets_category_id_effective_from",
                table: "category_budgets");

            migrationBuilder.DropColumn(
                name: "effective_from",
                table: "category_budgets");

            migrationBuilder.CreateIndex(
                name: "IX_category_budgets_category_id",
                table: "category_budgets",
                column: "category_id",
                unique: true);
        }
    }
}
