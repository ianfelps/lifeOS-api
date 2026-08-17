using ServiceLifeOS.Dtos.Finances;
using ServiceLifeOS.Dtos.Gamification;
using ServiceLifeOS.Dtos.Habits;
using ServiceLifeOS.Dtos.Workouts;

namespace ServiceLifeOS.Dtos.Dashboard;

public sealed class DashboardResponseDto
{
    public MonthlySummaryResponseDto Finance { get; set; } = new();
    public IReadOnlyCollection<HabitProgressResponseDto> PendingHabits { get; set; } = [];
    public IReadOnlyCollection<WorkoutSessionResponseDto> RecentWorkouts { get; set; } = [];
    public GamificationProfileResponseDto Gamification { get; set; } = new();
}
