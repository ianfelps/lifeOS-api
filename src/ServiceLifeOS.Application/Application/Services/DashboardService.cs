using ServiceLifeOS.Dtos.Dashboard;
using ServiceLifeOS.Dtos.Workouts;

namespace ServiceLifeOS.Application.Services;

public sealed class DashboardService
{
    private readonly FinanceService _finances;
    private readonly HabitService _habits;
    private readonly WorkoutService _workouts;
    private readonly GamificationService _gamification;

    public DashboardService(FinanceService finances, HabitService habits, WorkoutService workouts, GamificationService gamification)
    {
        _finances = finances;
        _habits = habits;
        _workouts = workouts;
        _gamification = gamification;
    }

    public async Task<DashboardResponseDto> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
        var month = new DateOnly(today.Year, today.Month, 1);
        var workouts = await _workouts.GetSessionsAsync(userId, new WorkoutSessionQueryDto { PageSize = 5 }, cancellationToken);
        return new()
        {
            Finance = await _finances.GetMonthlySummaryAsync(userId, month, cancellationToken),
            PendingHabits = await _habits.GetPendingHabitsAsync(userId, today, cancellationToken),
            RecentWorkouts = workouts.Items,
            Gamification = await _gamification.GetProfileAsync(userId, cancellationToken)
        };
    }
}
