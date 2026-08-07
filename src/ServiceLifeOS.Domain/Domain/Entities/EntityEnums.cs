namespace ServiceLifeOS.Domain.Entities;

public enum FinancialCategoryType { Income, Expense }
public enum PaymentMethod { Pix, Credit, Debit, InstallmentCredit }
public enum TransactionStatus { Planned, Confirmed, Overdue }
public enum HabitPriority { Low, Medium, High }
public enum HabitStatus { Active, Paused, Archived }
public enum HabitScheduleType { Daily, Weekdays, WeeklyCount, DailyCount }
public enum WorkoutSessionStatus { Draft, Completed, Cancelled }
public enum WeightUnit { Kilograms, Pounds }
public enum GoalType { Financial, Habit, Training, FreeForm }
public enum GoalStatus { Active, Completed, Cancelled }
public enum GoalSourceType { Category, Habit, Exercise, WorkoutSheet }
public enum XpEventType { HabitCompletion, WeeklyHabitGoal, WorkoutCompleted, TransactionConfirmed, PositiveMonth, GoalCompleted }
public enum XpLedgerEntryType { Grant, Reversal, Adjustment }
public enum BadgeCriterionType { Xp, Level, HabitCompletionCount, WeeklyHabitGoalCount, WorkoutCompletionCount, TransactionConfirmationCount, GoalCompletionCount, PositiveMonthCount }
public enum AuditAction { Created, Updated, Archived, Deleted, Login, PasswordChanged, SessionsRevoked }
