using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceLifeOS.Infrastructure.Persistence;

#nullable disable

namespace ServiceLifeOS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260911120000_AddWorkoutMuscleGroups")]
    public partial class AddWorkoutMuscleGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "primary_muscle_group",
                table: "exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.AddColumn<int>(
                name: "secondary_muscle_group",
                table: "exercises",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "primary_muscle_group",
                table: "workout_sheets",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "secondary_muscle_group",
                table: "workout_sheets",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "primary_muscle_group", table: "exercises");
            migrationBuilder.DropColumn(name: "secondary_muscle_group", table: "exercises");
            migrationBuilder.DropColumn(name: "primary_muscle_group", table: "workout_sheets");
            migrationBuilder.DropColumn(name: "secondary_muscle_group", table: "workout_sheets");
        }
    }
}
