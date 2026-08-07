using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceLifeOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    user_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: true),
                    action = table.Column<int>(type: "integer", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_values = table.Column<string>(type: "text", nullable: true),
                    current_values = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badges", x => x.id);
                    table.ForeignKey(
                        name: "FK_badges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.id);
                    table.ForeignKey(
                        name: "FK_exercises_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_financial_categories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    manual_progress = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goals", x => x.id);
                    table.ForeignKey(
                        name: "FK_goals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "habits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habits", x => x.id);
                    table.ForeignKey(
                        name: "FK_habits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "level_progression_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    base_xp = table.Column<int>(type: "integer", nullable: false),
                    increment_per_level = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_level_progression_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_level_progression_rules_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    preferred_weight_unit = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    token_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sheets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sheets", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sheets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xp_event_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_event_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_xp_event_rules_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "xp_ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xp_ledger_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_xp_ledger_entries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    badge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_badges", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_badges_badges_badge_id",
                        column: x => x.badge_id,
                        principalTable: "badges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_badges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_budgets", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_budgets_financial_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "financial_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installment_purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    installment_count = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_installment_purchases", x => x.id);
                    table.ForeignKey(
                        name: "FK_installment_purchases_financial_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "financial_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_installment_purchases_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurring_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    first_occurrence_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_recurring_transactions_financial_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "financial_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goal_source_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goal_source_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_goal_source_links_goals_goal_id",
                        column: x => x.goal_id,
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "badge_criteria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    target_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    habit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    financial_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_criteria", x => x.id);
                    table.ForeignKey(
                        name: "FK_badge_criteria_badges_badge_id",
                        column: x => x.badge_id,
                        principalTable: "badges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_badge_criteria_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_badge_criteria_financial_categories_financial_category_id",
                        column: x => x.financial_category_id,
                        principalTable: "financial_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_badge_criteria_goals_goal_id",
                        column: x => x.goal_id,
                        principalTable: "goals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_badge_criteria_habits_habit_id",
                        column: x => x.habit_id,
                        principalTable: "habits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "habit_completions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    habit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_on = table.Column<DateOnly>(type: "date", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_completions", x => x.id);
                    table.ForeignKey(
                        name: "FK_habit_completions_habits_habit_id",
                        column: x => x.habit_id,
                        principalTable: "habits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_habit_completions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "habit_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    habit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    target_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_habit_schedules_habits_habit_id",
                        column: x => x.habit_id,
                        principalTable: "habits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    workout_sheet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_workout_sheets_workout_sheet_id",
                        column: x => x.workout_sheet_id,
                        principalTable: "workout_sheets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workout_sheet_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_sheet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sheet_exercises", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sheet_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_sheet_exercises_workout_sheets_workout_sheet_id",
                        column: x => x.workout_sheet_id,
                        principalTable: "workout_sheets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_budget_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_budget_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_category_budget_overrides_category_budgets_category_budget_~",
                        column: x => x.category_budget_id,
                        principalTable: "category_budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financial_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(120)", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recurring_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    installment_purchase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_financial_transactions_financial_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "financial_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_transactions_installment_purchases_installment_pu~",
                        column: x => x.installment_purchase_id,
                        principalTable: "installment_purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_financial_transactions_recurring_transactions_recurring_tra~",
                        column: x => x.recurring_transaction_id,
                        principalTable: "recurring_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_financial_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "habit_schedule_weekdays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    habit_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_habit_schedule_weekdays", x => x.id);
                    table.ForeignKey(
                        name: "FK_habit_schedule_weekdays_habit_schedules_habit_schedule_id",
                        column: x => x.habit_schedule_id,
                        principalTable: "habit_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_exercises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    exercise_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exercise_name = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_exercises", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_workout_sessions_workout_session_~",
                        column: x => x.workout_session_id,
                        principalTable: "workout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sheet_exercise_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_sheet_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    target_repetitions = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sheet_exercise_sets", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_sheet_exercise_sets_workout_sheet_exercises_workout~",
                        column: x => x.workout_sheet_exercise_id,
                        principalTable: "workout_sheet_exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workout_session_exercise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    weight_unit = table.Column<int>(type: "integer", nullable: true),
                    repetitions = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_sets", x => x.id);
                    table.ForeignKey(
                        name: "FK_workout_session_sets_workout_session_exercises_workout_sess~",
                        column: x => x.workout_session_exercise_id,
                        principalTable: "workout_session_exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_criteria_badge_id",
                table: "badge_criteria",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_criteria_exercise_id",
                table: "badge_criteria",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_criteria_financial_category_id",
                table: "badge_criteria",
                column: "financial_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_criteria_goal_id",
                table: "badge_criteria",
                column: "goal_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_criteria_habit_id",
                table: "badge_criteria",
                column: "habit_id");

            migrationBuilder.CreateIndex(
                name: "IX_badges_user_id",
                table: "badges",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_budget_overrides_category_budget_id",
                table: "category_budget_overrides",
                column: "category_budget_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_budget_overrides_category_budget_id_month",
                table: "category_budget_overrides",
                columns: new[] { "category_budget_id", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_budgets_category_id",
                table: "category_budgets",
                column: "category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_user_id",
                table: "exercises",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_user_id",
                table: "financial_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_categories_user_id_name_type",
                table: "financial_categories",
                columns: new[] { "user_id", "name", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_category_id",
                table: "financial_transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_installment_purchase_id",
                table: "financial_transactions",
                column: "installment_purchase_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_recurring_transaction_id",
                table: "financial_transactions",
                column: "recurring_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_user_id",
                table: "financial_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_goal_source_links_goal_id",
                table: "goal_source_links",
                column: "goal_id");

            migrationBuilder.CreateIndex(
                name: "IX_goals_user_id",
                table: "goals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_habit_completions_habit_id",
                table: "habit_completions",
                column: "habit_id");

            migrationBuilder.CreateIndex(
                name: "IX_habit_completions_user_id",
                table: "habit_completions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_habit_schedule_weekdays_habit_schedule_id",
                table: "habit_schedule_weekdays",
                column: "habit_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_habit_schedules_habit_id",
                table: "habit_schedules",
                column: "habit_id");

            migrationBuilder.CreateIndex(
                name: "IX_habits_user_id",
                table: "habits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_installment_purchases_category_id",
                table: "installment_purchases",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_installment_purchases_user_id",
                table: "installment_purchases",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_level_progression_rules_user_id",
                table: "level_progression_rules",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_transactions_category_id",
                table: "recurring_transactions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_transactions_user_id",
                table: "recurring_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_badge_id",
                table: "user_badges",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_user_id",
                table: "user_badges",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_user_id_badge_id",
                table: "user_badges",
                columns: new[] { "user_id", "badge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_user_id",
                table: "user_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_token_id",
                table: "user_sessions",
                column: "token_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_user_id",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_user_name",
                table: "users",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_exercise_id",
                table: "workout_session_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_workout_session_id",
                table: "workout_session_exercises",
                column: "workout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_workout_session_id_position",
                table: "workout_session_exercises",
                columns: new[] { "workout_session_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_sets_workout_session_exercise_id",
                table: "workout_session_sets",
                column: "workout_session_exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_sets_workout_session_exercise_id_position",
                table: "workout_session_sets",
                columns: new[] { "workout_session_exercise_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_user_id",
                table: "workout_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_workout_sheet_id",
                table: "workout_sessions",
                column: "workout_sheet_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheet_exercise_sets_workout_sheet_exercise_id",
                table: "workout_sheet_exercise_sets",
                column: "workout_sheet_exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheet_exercise_sets_workout_sheet_exercise_id_posit~",
                table: "workout_sheet_exercise_sets",
                columns: new[] { "workout_sheet_exercise_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheet_exercises_exercise_id",
                table: "workout_sheet_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheet_exercises_workout_sheet_id",
                table: "workout_sheet_exercises",
                column: "workout_sheet_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheet_exercises_workout_sheet_id_position",
                table: "workout_sheet_exercises",
                columns: new[] { "workout_sheet_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sheets_user_id",
                table: "workout_sheets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_xp_event_rules_user_id",
                table: "xp_event_rules",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_xp_event_rules_user_id_event_type",
                table: "xp_event_rules",
                columns: new[] { "user_id", "event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xp_ledger_entries_user_id",
                table: "xp_ledger_entries",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "badge_criteria");

            migrationBuilder.DropTable(
                name: "category_budget_overrides");

            migrationBuilder.DropTable(
                name: "financial_transactions");

            migrationBuilder.DropTable(
                name: "goal_source_links");

            migrationBuilder.DropTable(
                name: "habit_completions");

            migrationBuilder.DropTable(
                name: "habit_schedule_weekdays");

            migrationBuilder.DropTable(
                name: "level_progression_rules");

            migrationBuilder.DropTable(
                name: "user_badges");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "workout_session_sets");

            migrationBuilder.DropTable(
                name: "workout_sheet_exercise_sets");

            migrationBuilder.DropTable(
                name: "xp_event_rules");

            migrationBuilder.DropTable(
                name: "xp_ledger_entries");

            migrationBuilder.DropTable(
                name: "category_budgets");

            migrationBuilder.DropTable(
                name: "installment_purchases");

            migrationBuilder.DropTable(
                name: "recurring_transactions");

            migrationBuilder.DropTable(
                name: "goals");

            migrationBuilder.DropTable(
                name: "habit_schedules");

            migrationBuilder.DropTable(
                name: "badges");

            migrationBuilder.DropTable(
                name: "workout_session_exercises");

            migrationBuilder.DropTable(
                name: "workout_sheet_exercises");

            migrationBuilder.DropTable(
                name: "financial_categories");

            migrationBuilder.DropTable(
                name: "habits");

            migrationBuilder.DropTable(
                name: "workout_sessions");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workout_sheets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
