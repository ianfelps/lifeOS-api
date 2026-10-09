using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceLifeOS.Infrastructure.Persistence;

#nullable disable

namespace ServiceLifeOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261009090000_AddBillingCycleStartDay")]
    public partial class AddBillingCycleStartDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "billing_cycle_start_day",
                table: "user_preferences",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "billing_cycle_start_day", table: "user_preferences");
        }
    }
}
