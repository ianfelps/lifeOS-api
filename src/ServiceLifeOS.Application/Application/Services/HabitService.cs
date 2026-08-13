using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Habits;

namespace ServiceLifeOS.Application.Services;

public sealed class HabitService
{
    private const string CompletionSourceType = "HabitCompletion";
    private const string WeeklyGoalSourceType = "HabitWeeklyGoal";
    private readonly IHabitRepository _habits;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public HabitService(IHabitRepository habits, IAuditLogRepository auditLogs, IUnitOfWork unitOfWork)
    {
        _habits = habits;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedHabitResponseDto> GetHabitsAsync(string userId, HabitQueryDto query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))) throw new ArgumentException("Habit query is invalid.");
        var habits = (await _habits.GetHabitsAsync(userId, cancellationToken)).Where(x => query.IncludeArchived || x.Status != HabitStatus.Archived);
        if (query.Status.HasValue) habits = habits.Where(x => x.Status == query.Status.Value);
        var values = habits.OrderBy(x => x.Title).ThenBy(x => x.Id).ToArray();
        var items = new List<HabitResponseDto>();
        foreach (var habit in values.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)) items.Add(await MapHabitAsync(habit, cancellationToken));
        return new() { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = values.Length };
    }

    public async Task<HabitResponseDto> GetHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => await MapHabitAsync(await RequiredHabitAsync(userId, habitId, cancellationToken), cancellationToken);

    public async Task<HabitResponseDto> CreateHabitAsync(string userId, HabitRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var now = DateTime.UtcNow;
        var habit = new Habit { UserId = userId, Title = request.Title.Trim(), Priority = request.Priority, CreatedAt = now, UpdatedAt = now };
        var schedule = CreateSchedule(habit.Id, request.Schedule, now);
        await _habits.AddAsync(habit, cancellationToken);
        await _habits.AddAsync(schedule, cancellationToken);
        await AddWeekdaysAsync(schedule.Id, request.Schedule.Weekdays, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "Habit", habit.Id, null, habit, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapHabitAsync(habit, schedule, request.Schedule.Weekdays, cancellationToken);
    }

    public async Task<HabitResponseDto> UpdateHabitAsync(string userId, Guid habitId, HabitRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var habit = await RequiredHabitAsync(userId, habitId, cancellationToken);
        var previous = await MapHabitAsync(habit, cancellationToken);
        var oldSchedule = await _habits.GetScheduleAsync(habit.Id, cancellationToken);
        if (oldSchedule is not null)
        {
            var oldWeekdays = await _habits.GetWeekdaysAsync(oldSchedule.Id, cancellationToken);
            foreach (var weekday in oldWeekdays) await _habits.RemoveAsync(weekday, cancellationToken);
            await _habits.RemoveAsync(oldSchedule, cancellationToken);
        }
        var now = DateTime.UtcNow;
        habit.Title = request.Title.Trim();
        habit.Priority = request.Priority;
        habit.UpdatedAt = now;
        var schedule = CreateSchedule(habit.Id, request.Schedule, now);
        await _habits.AddAsync(schedule, cancellationToken);
        await AddWeekdaysAsync(schedule.Id, request.Schedule.Weekdays, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "Habit", habit.Id, previous, habit, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapHabitAsync(habit, schedule, request.Schedule.Weekdays, cancellationToken);
    }

    public Task PauseHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => SetStatusAsync(userId, habitId, HabitStatus.Paused, AuditAction.Updated, cancellationToken);
    public Task ResumeHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => SetStatusAsync(userId, habitId, HabitStatus.Active, AuditAction.Updated, cancellationToken);
    public Task ArchiveHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => SetStatusAsync(userId, habitId, HabitStatus.Archived, AuditAction.Archived, cancellationToken);

    public async Task<IReadOnlyCollection<HabitCompletionResponseDto>> GetCompletionsAsync(string userId, Guid habitId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to) throw new ArgumentException("Completion period is invalid.");
        await RequiredHabitAsync(userId, habitId, cancellationToken);
        return (await _habits.GetCompletionsAsync(userId, habitId, cancellationToken)).Where(x => x.DeletedAt is null && x.CompletedOn >= from && x.CompletedOn <= to).OrderBy(x => x.CompletedOn).ThenBy(x => x.Id).Select(MapCompletion).ToArray();
    }

    public async Task<HabitCompletionResponseDto> CreateCompletionAsync(string userId, Guid habitId, HabitCompletionRequestDto request, CancellationToken cancellationToken = default)
    {
        var habit = await RequiredHabitAsync(userId, habitId, cancellationToken);
        if (habit.Status != HabitStatus.Active) throw new InvalidOperationException("Only active habits can be completed.");
        ValidateCompletionDate(request.CompletedOn);
        var schedule = await RequiredScheduleAsync(habit.Id, cancellationToken);
        var weekdays = await _habits.GetWeekdaysAsync(schedule.Id, cancellationToken);
        var completions = (await _habits.GetCompletionsAsync(userId, habitId, cancellationToken)).Where(x => x.DeletedAt is null).ToArray();
        if (!IsScheduledOn(schedule, weekdays, request.CompletedOn) || CompletionCountForPeriod(schedule, completions, request.CompletedOn) >= schedule.TargetCount) throw new ArgumentException("The completion is outside the schedule or exceeds its target.");
        var now = DateTime.UtcNow;
        var completion = new HabitCompletion { UserId = userId, HabitId = habitId, CompletedOn = request.CompletedOn, CreatedAt = now };
        await _habits.AddAsync(completion, cancellationToken);
        await SyncCompletionXpAsync(userId, completion, true, now, cancellationToken);
        await SyncWeeklyGoalXpAsync(userId, habitId, schedule, completions.Append(completion), request.CompletedOn, now, cancellationToken);
        await RecalculateHabitBadgesAsync(userId, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "HabitCompletion", completion.Id, null, completion, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCompletion(completion);
    }

    public async Task DeleteCompletionAsync(string userId, Guid habitId, Guid completionId, CancellationToken cancellationToken = default)
    {
        var habit = await RequiredHabitAsync(userId, habitId, cancellationToken);
        var completion = await _habits.GetCompletionAsync(userId, habitId, completionId, cancellationToken) ?? throw new KeyNotFoundException("Habit completion was not found.");
        ValidateCompletionDate(completion.CompletedOn);
        var schedule = await RequiredScheduleAsync(habit.Id, cancellationToken);
        var now = DateTime.UtcNow;
        completion.DeletedAt = now;
        await SyncCompletionXpAsync(userId, completion, false, now, cancellationToken);
        var completions = (await _habits.GetCompletionsAsync(userId, habitId, cancellationToken)).Where(x => x.DeletedAt is null && x.Id != completion.Id).ToArray();
        await SyncWeeklyGoalXpAsync(userId, habitId, schedule, completions, completion.CompletedOn, now, cancellationToken);
        await RecalculateHabitBadgesAsync(userId, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Deleted, "HabitCompletion", completion.Id, completion, null, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<HabitProgressResponseDto> GetProgressAsync(string userId, Guid habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var habit = await RequiredHabitAsync(userId, habitId, cancellationToken);
        var schedule = await RequiredScheduleAsync(habit.Id, cancellationToken);
        var weekdays = await _habits.GetWeekdaysAsync(schedule.Id, cancellationToken);
        var completions = (await _habits.GetCompletionsAsync(userId, habitId, cancellationToken)).Where(x => x.DeletedAt is null).ToArray();
        var period = GetPeriod(schedule, date);
        var completionCount = completions.Count(x => x.CompletedOn >= period.Start && x.CompletedOn <= period.End);
        return new() { HabitId = habitId, PeriodStart = period.Start, PeriodEnd = period.End, CompletionCount = completionCount, TargetCount = schedule.TargetCount, IsCompleted = completionCount >= schedule.TargetCount, Streak = CalculateStreak(schedule, weekdays, completions, date) };
    }

    public async Task<IReadOnlyCollection<HabitProgressResponseDto>> GetPendingHabitsAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var result = new List<HabitProgressResponseDto>();
        foreach (var habit in (await _habits.GetHabitsAsync(userId, cancellationToken)).Where(x => x.Status == HabitStatus.Active))
        {
            var schedule = await RequiredScheduleAsync(habit.Id, cancellationToken);
            var weekdays = await _habits.GetWeekdaysAsync(schedule.Id, cancellationToken);
            if (!IsScheduledOn(schedule, weekdays, date)) continue;
            var progress = await GetProgressAsync(userId, habit.Id, date, cancellationToken);
            if (!progress.IsCompleted) result.Add(progress);
        }
        return result;
    }

    private async Task SetStatusAsync(string userId, Guid habitId, HabitStatus status, AuditAction action, CancellationToken cancellationToken)
    {
        var habit = await RequiredHabitAsync(userId, habitId, cancellationToken);
        if (habit.Status == status) return;
        var previous = habit.Status;
        habit.Status = status;
        habit.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, action, "Habit", habit.Id, previous, habit, habit.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncCompletionXpAsync(string userId, HabitCompletion completion, bool qualifies, DateTime now, CancellationToken cancellationToken)
    {
        var entries = await _habits.GetXpEntriesForSourceAsync(userId, CompletionSourceType, completion.Id, cancellationToken);
        var grant = ActiveGrant(entries);
        if (qualifies && grant is null)
        {
            var rule = await _habits.GetXpRuleAsync(userId, XpEventType.HabitCompletion, cancellationToken);
            if (rule is not null) await _habits.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Grant, Amount = rule.Amount, EventType = XpEventType.HabitCompletion, SourceType = CompletionSourceType, SourceId = completion.Id, CreatedAt = now }, cancellationToken);
        }
        if (!qualifies && grant is not null) await _habits.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Reversal, Amount = -grant.Amount, EventType = XpEventType.HabitCompletion, SourceType = CompletionSourceType, SourceId = completion.Id, ReversedEntryId = grant.Id, CreatedAt = now }, cancellationToken);
    }

    private async Task SyncWeeklyGoalXpAsync(string userId, Guid habitId, HabitSchedule schedule, IEnumerable<HabitCompletion> completions, DateOnly date, DateTime now, CancellationToken cancellationToken)
    {
        if (schedule.Type != HabitScheduleType.WeeklyCount) return;
        var week = GetPeriod(schedule, date);
        var sourceId = WeeklyGoalId(habitId, week.Start);
        var entries = await _habits.GetXpEntriesForSourceAsync(userId, WeeklyGoalSourceType, sourceId, cancellationToken);
        var grant = ActiveGrant(entries);
        var qualifies = completions.Count(x => x.DeletedAt is null && x.CompletedOn >= week.Start && x.CompletedOn <= week.End) >= schedule.TargetCount;
        if (qualifies && grant is null)
        {
            var rule = await _habits.GetXpRuleAsync(userId, XpEventType.WeeklyHabitGoal, cancellationToken);
            if (rule is not null) await _habits.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Grant, Amount = rule.Amount, EventType = XpEventType.WeeklyHabitGoal, SourceType = WeeklyGoalSourceType, SourceId = sourceId, CreatedAt = now }, cancellationToken);
        }
        if (!qualifies && grant is not null) await _habits.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Reversal, Amount = -grant.Amount, EventType = XpEventType.WeeklyHabitGoal, SourceType = WeeklyGoalSourceType, SourceId = sourceId, ReversedEntryId = grant.Id, CreatedAt = now }, cancellationToken);
    }

    private async Task RecalculateHabitBadgesAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var badges = await _habits.GetBadgesAsync(userId, cancellationToken);
        var criteria = await _habits.GetBadgeCriteriaAsync(badges.Select(x => x.Id).ToArray(), cancellationToken);
        var completionEntries = await _habits.GetXpEntriesAsync(userId, CompletionSourceType, cancellationToken);
        var weeklyGoalEntries = await _habits.GetXpEntriesAsync(userId, WeeklyGoalSourceType, cancellationToken);
        var completions = completionEntries.Count(x => x.Type == XpLedgerEntryType.Grant && !completionEntries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
        var weeklyGoals = weeklyGoalEntries.Count(x => x.Type == XpLedgerEntryType.Grant && !weeklyGoalEntries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
        var unlocked = await _habits.GetUserBadgesAsync(userId, cancellationToken);
        foreach (var badge in badges)
        {
            var badgeCriteria = criteria.Where(x => x.BadgeId == badge.Id).ToArray();
            if (badgeCriteria.Length == 0 || badgeCriteria.Any(x => x.Type is not (BadgeCriterionType.HabitCompletionCount or BadgeCriterionType.WeeklyHabitGoalCount))) continue;
            var meets = badgeCriteria.All(x => x.Type == BadgeCriterionType.HabitCompletionCount ? completions >= x.TargetValue : weeklyGoals >= x.TargetValue);
            var existing = unlocked.FirstOrDefault(x => x.BadgeId == badge.Id);
            if (meets && existing is null) await _habits.AddAsync(new UserBadge { UserId = userId, BadgeId = badge.Id, UnlockedAt = now }, cancellationToken);
            if (!meets && existing is not null) await _habits.RemoveAsync(existing, cancellationToken);
        }
    }

    private async Task AddWeekdaysAsync(Guid scheduleId, IEnumerable<DayOfWeek> weekdays, CancellationToken cancellationToken) => await _habits.AddRangeAsync(weekdays.Distinct().Select(x => new HabitScheduleWeekday { HabitScheduleId = scheduleId, DayOfWeek = x }), cancellationToken);
    private async Task<Habit> RequiredHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken) => await _habits.GetHabitAsync(userId, habitId, cancellationToken) ?? throw new KeyNotFoundException("Habit was not found.");
    private async Task<HabitSchedule> RequiredScheduleAsync(Guid habitId, CancellationToken cancellationToken) => await _habits.GetScheduleAsync(habitId, cancellationToken) ?? throw new InvalidOperationException("Habit schedule was not found.");
    private async Task<HabitResponseDto> MapHabitAsync(Habit habit, CancellationToken cancellationToken) { var schedule = await RequiredScheduleAsync(habit.Id, cancellationToken); return await MapHabitAsync(habit, schedule, (await _habits.GetWeekdaysAsync(schedule.Id, cancellationToken)).Select(x => x.DayOfWeek), cancellationToken); }
    private static Task<HabitResponseDto> MapHabitAsync(Habit habit, HabitSchedule schedule, IEnumerable<DayOfWeek> weekdays, CancellationToken cancellationToken) => Task.FromResult(new HabitResponseDto { Id = habit.Id, Title = habit.Title, Priority = habit.Priority, Status = habit.Status, Schedule = new() { Type = schedule.Type, TargetCount = schedule.TargetCount, Weekdays = weekdays.OrderBy(x => x).ToArray() } });
    private static HabitCompletionResponseDto MapCompletion(HabitCompletion value) => new() { Id = value.Id, HabitId = value.HabitId, CompletedOn = value.CompletedOn };
    private static HabitSchedule CreateSchedule(Guid habitId, HabitScheduleRequestDto request, DateTime now) => new() { HabitId = habitId, Type = request.Type, TargetCount = request.Type is HabitScheduleType.Daily or HabitScheduleType.Weekdays ? 1 : request.TargetCount, CreatedAt = now, UpdatedAt = now };
    private static XpLedgerEntry? ActiveGrant(IEnumerable<XpLedgerEntry> entries) => entries.FirstOrDefault(x => x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
    private static bool IsScheduledOn(HabitSchedule schedule, IReadOnlyCollection<HabitScheduleWeekday> weekdays, DateOnly date) => schedule.Type != HabitScheduleType.Weekdays || weekdays.Any(x => x.DayOfWeek == date.DayOfWeek);
    private static int CompletionCountForPeriod(HabitSchedule schedule, IEnumerable<HabitCompletion> completions, DateOnly date) { var period = GetPeriod(schedule, date); return completions.Count(x => x.CompletedOn >= period.Start && x.CompletedOn <= period.End); }
    private static (DateOnly Start, DateOnly End) GetPeriod(HabitSchedule schedule, DateOnly date) { var start = schedule.Type == HabitScheduleType.WeeklyCount ? date.AddDays(-((int)date.DayOfWeek + 6) % 7) : date; return (start, schedule.Type == HabitScheduleType.WeeklyCount ? start.AddDays(6) : start); }
    private static int CalculateStreak(HabitSchedule schedule, IReadOnlyCollection<HabitScheduleWeekday> weekdays, IReadOnlyCollection<HabitCompletion> completions, DateOnly date)
    {
        var count = 0;
        var current = schedule.Type == HabitScheduleType.WeeklyCount ? GetPeriod(schedule, date).Start : date;
        while (true)
        {
            if (schedule.Type == HabitScheduleType.Weekdays && !IsScheduledOn(schedule, weekdays, current)) { current = current.AddDays(-1); continue; }
            var period = GetPeriod(schedule, current);
            if (completions.Count(x => x.CompletedOn >= period.Start && x.CompletedOn <= period.End) < schedule.TargetCount) return count;
            count++;
            current = schedule.Type == HabitScheduleType.WeeklyCount ? current.AddDays(-7) : current.AddDays(-1);
        }
    }
    private async Task AuditAsync(string userId, AuditAction action, string resourceType, Guid resourceId, object? previous, object? current, DateTime now, CancellationToken cancellationToken) => await _auditLogs.CreateAsync(new AuditLog { UserId = userId, Action = action, ResourceType = resourceType, ResourceId = resourceId, PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous), CurrentValues = current is null ? null : JsonSerializer.Serialize(current), CreatedAt = now }, cancellationToken);
    private static void ValidateRequest(HabitRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 120 || !Enum.IsDefined(request.Priority) || request.Schedule is null || !Enum.IsDefined(request.Schedule.Type)) throw new ArgumentException("Habit is invalid.");
        var weekdays = request.Schedule.Weekdays ?? [];
        if (weekdays.Any(x => !Enum.IsDefined(x)) || weekdays.Distinct().Count() != weekdays.Count) throw new ArgumentException("Habit schedule is invalid.");
        if (request.Schedule.Type == HabitScheduleType.Weekdays && weekdays.Count == 0) throw new ArgumentException("Weekday schedules require at least one weekday.");
        if (request.Schedule.Type != HabitScheduleType.Weekdays && weekdays.Count != 0) throw new ArgumentException("Only weekday schedules accept weekdays.");
        if (request.Schedule.Type is HabitScheduleType.WeeklyCount or HabitScheduleType.DailyCount && request.Schedule.TargetCount < 1) throw new ArgumentException("Habit target count must be greater than zero.");
    }
    private static void ValidateCompletionDate(DateOnly date) { var today = LocalToday(); if (date > today || date < today.AddDays(-7)) throw new ArgumentException("Habit completions must be within the last seven days."); }
    private static DateOnly LocalToday() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
    private static Guid WeeklyGoalId(Guid habitId, DateOnly weekStart) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{habitId:N}:{weekStart:yyyyMMdd}"))[..16]);
}
