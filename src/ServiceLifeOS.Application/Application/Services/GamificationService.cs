using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Gamification;

namespace ServiceLifeOS.Application.Services;

public sealed class GamificationService
{
    private const string GoalSourceTypeName = "Goal";
    private const string PositiveMonthSourceType = "PositiveMonth";
    private readonly IGamificationRepository _gamification;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogRepository? _auditLogs;

    public GamificationService(
        IGamificationRepository gamification,
        IUnitOfWork unitOfWork,
        IAuditLogRepository? auditLogs = null)
    {
        _gamification = gamification;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task<GamificationProfileResponseDto> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        await RefreshAsync(userId, cancellationToken);
        var entries = await _gamification.GetXpEntriesAsync(userId, cancellationToken);
        var rule = await _gamification.GetLevelRuleAsync(userId, cancellationToken);
        var badges = await _gamification.GetBadgesAsync(userId, cancellationToken);
        var criteria = await _gamification.GetBadgeCriteriaAsync(
            badges.Select(x => x.Id).ToArray(),
            cancellationToken);
        var unlocked = await _gamification.GetUserBadgesAsync(userId, cancellationToken);
        var totalXp = entries.Sum(x => x.Amount);
        var level = CalculateLevel(totalXp, rule);
        return new()
        {
            TotalXp = totalXp,
            Level = level,
            CurrentLevelXp = LevelThreshold(level, rule),
            NextLevelXp = rule is null ? null : LevelThreshold(level + 1, rule),
            Badges = badges.OrderBy(x => x.Name).Select(x => MapBadge(x, criteria, unlocked)).ToArray()
        };
    }

    public async Task<PagedXpLedgerResponseDto> GetLedgerAsync(
        string userId,
        XpLedgerQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        var entries = (await _gamification.GetXpEntriesAsync(userId, cancellationToken))
            .Where(x => !query.EventType.HasValue || x.EventType == query.EventType)
            .Where(x => !query.From.HasValue || x.CreatedAt >= query.From)
            .Where(x => !query.To.HasValue || x.CreatedAt <= query.To)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToArray();
        return new()
        {
            Items = entries
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(MapLedger)
                .ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = entries.Length
        };
    }

    public async Task<PagedGoalResponseDto> GetGoalsAsync(
        string userId,
        GoalQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        await RefreshAsync(userId, cancellationToken);
        var goals = (await _gamification.GetGoalsAsync(userId, cancellationToken))
            .Where(x => query.IncludeArchived || !x.Archived)
            .Where(x => !query.Status.HasValue || x.Status == query.Status)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Title)
            .ToArray();
        var sources = await _gamification.GetGoalSourcesAsync(
            goals.Select(x => x.Id).ToArray(),
            cancellationToken);
        var pageGoals = goals
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        var items = new List<GoalResponseDto>();
        foreach (var goal in pageGoals)
        {
            var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
            items.Add(MapGoal(goal, sources, progress));
        }
        return new()
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = goals.Length
        };
    }

    public async Task<GoalResponseDto> GetGoalAsync(
        string userId,
        Guid goalId,
        CancellationToken cancellationToken = default)
    {
        await RefreshAsync(userId, cancellationToken);
        var goal = await RequiredGoalAsync(userId, goalId, cancellationToken);
        var sources = await _gamification.GetGoalSourcesAsync([goal.Id], cancellationToken);
        var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
        return MapGoal(goal, sources, progress);
    }

    public async Task<GoalResponseDto> CreateGoalAsync(
        string userId,
        GoalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateGoalRequest(request);
        await ValidateGoalSourcesAsync(userId, request.Type, request.Sources, cancellationToken);
        var now = DateTime.UtcNow;
        var goal = new Goal
        {
            UserId = userId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = TrimToNull(request.Description),
            TargetValue = request.TargetValue,
            Unit = request.Unit.Trim(),
            DueDate = request.DueDate,
            ManualProgress = request.Type == GoalType.FreeForm ? 0 : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _gamification.AddAsync(goal, cancellationToken);
        var sources = request.Sources
            .Select(x => new GoalSourceLink
            {
                GoalId = goal.Id,
                SourceType = x.SourceType,
                SourceId = x.SourceId
            })
            .ToArray();
        await _gamification.AddRangeAsync(sources, cancellationToken);
        await AuditAsync(
            userId,
            AuditAction.Created,
            "Goal",
            goal.Id,
            null,
            goal,
            now,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
        return MapGoal(goal, sources, progress);
    }

    public async Task<GoalResponseDto> UpdateGoalAsync(
        string userId,
        Guid goalId,
        GoalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateGoalRequest(request);
        var goal = await RequiredGoalAsync(userId, goalId, cancellationToken);
        if (goal.Type != request.Type)
        {
            throw new ArgumentException("Goal type cannot be changed.");
        }
        await ValidateGoalSourcesAsync(userId, request.Type, request.Sources, cancellationToken);
        var existingSources = await _gamification.GetGoalSourcesAsync([goal.Id], cancellationToken);
        foreach (var source in existingSources)
        {
            await _gamification.RemoveAsync(source, cancellationToken);
        }

        var sources = request.Sources
            .Select(x => new GoalSourceLink
            {
                GoalId = goal.Id,
                SourceType = x.SourceType,
                SourceId = x.SourceId
            })
            .ToArray();
        await _gamification.AddRangeAsync(sources, cancellationToken);
        goal.Title = request.Title.Trim();
        goal.Description = TrimToNull(request.Description);
        goal.TargetValue = request.TargetValue;
        goal.Unit = request.Unit.Trim();
        goal.DueDate = request.DueDate;
        goal.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(
            userId,
            AuditAction.Updated,
            "Goal",
            goal.Id,
            null,
            goal,
            goal.UpdatedAt,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
        return MapGoal(goal, sources, progress);
    }

    public async Task<GoalResponseDto> UpdateManualProgressAsync(
        string userId,
        Guid goalId,
        ManualGoalProgressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Progress < 0)
        {
            throw new ArgumentException("Goal progress cannot be negative.");
        }
        var goal = await RequiredGoalAsync(userId, goalId, cancellationToken);
        if (goal.Type != GoalType.FreeForm)
        {
            throw new InvalidOperationException("Only free-form goals accept manual progress.");
        }

        if (goal.Status == GoalStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled goals cannot be updated.");
        }
        goal.ManualProgress = request.Progress;
        goal.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(
            userId,
            AuditAction.Updated,
            "Goal",
            goal.Id,
            null,
            goal,
            goal.UpdatedAt,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        var sources = await _gamification.GetGoalSourcesAsync([goal.Id], cancellationToken);
        var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
        return MapGoal(goal, sources, progress);
    }

    public async Task CancelGoalAsync(string userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await RequiredGoalAsync(userId, goalId, cancellationToken);
        goal.Status = GoalStatus.Cancelled;
        goal.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Updated, "Goal", goal.Id, null, goal, goal.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
    }

    public async Task ArchiveGoalAsync(string userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await RequiredGoalAsync(userId, goalId, cancellationToken);
        goal.Archived = true;
        goal.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Archived, "Goal", goal.Id, null, goal, goal.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<XpEventRuleRequestDto>> GetXpRulesAsync(string userId, CancellationToken cancellationToken = default) =>
        (await _gamification.GetXpRulesAsync(userId, cancellationToken)).OrderBy(x => x.EventType).Select(MapRule).ToArray();

    public async Task<IReadOnlyCollection<XpEventRuleRequestDto>> UpdateXpRulesAsync(string userId, IReadOnlyCollection<XpEventRuleRequestDto> requests, CancellationToken cancellationToken = default)
    {
        if (requests.Count != Enum.GetValues<XpEventType>().Length ||
            requests.Select(x => x.EventType).Distinct().Count() != requests.Count ||
            requests.Any(x => !Enum.IsDefined(x.EventType) || x.Amount < 0))
        {
            throw new ArgumentException("XP rules are invalid.");
        }
        var rules = await _gamification.GetXpRulesAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var request in requests)
        {
            var rule = rules.FirstOrDefault(x => x.EventType == request.EventType);
            if (rule is null)
            {
                await _gamification.AddAsync(
                    new XpEventRule
                    {
                        UserId = userId,
                        EventType = request.EventType,
                        Amount = request.Amount,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    cancellationToken);
            }
            else
            {
                rule.Amount = request.Amount;
                rule.UpdatedAt = now;
            }
        }
        await AuditAsync(userId, AuditAction.Updated, "XpEventRule", null, null, requests, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetXpRulesAsync(userId, cancellationToken);
    }

    public async Task<LevelProgressionRuleRequestDto> GetLevelRuleAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rule = await _gamification.GetLevelRuleAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Level progression rule was not found.");
        return new() { BaseXp = rule.BaseXp, IncrementPerLevel = rule.IncrementPerLevel };
    }

    public async Task<LevelProgressionRuleRequestDto> UpdateLevelRuleAsync(string userId, LevelProgressionRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.BaseXp < 1 || request.IncrementPerLevel < 0)
        {
            throw new ArgumentException("Level progression rule is invalid.");
        }

        var rule = await _gamification.GetLevelRuleAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Level progression rule was not found.");
        rule.BaseXp = request.BaseXp;
        rule.IncrementPerLevel = request.IncrementPerLevel;
        rule.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Updated, "LevelProgressionRule", rule.Id, null, rule, rule.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        return await GetLevelRuleAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BadgeResponseDto>> GetBadgesAsync(string userId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        await RefreshAsync(userId, cancellationToken);
        var badges = (await _gamification.GetBadgesAsync(userId, cancellationToken))
            .Where(x => includeArchived || !x.Archived)
            .OrderBy(x => x.Name)
            .ToArray();
        var criteria = await _gamification.GetBadgeCriteriaAsync(
            badges.Select(x => x.Id).ToArray(),
            cancellationToken);
        var unlocked = await _gamification.GetUserBadgesAsync(userId, cancellationToken);
        return badges.Select(x => MapBadge(x, criteria, unlocked)).ToArray();
    }

    public async Task<BadgeResponseDto> CreateBadgeAsync(string userId, BadgeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateBadgeRequest(request);
        await ValidateBadgeCriteriaAsync(userId, request.Criteria, cancellationToken);
        var now = DateTime.UtcNow;
        var badge = new Badge
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _gamification.AddAsync(badge, cancellationToken);
        var criteria = request.Criteria.Select(x => MapCriterion(badge.Id, x)).ToArray();
        await _gamification.AddRangeAsync(criteria, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "Badge", badge.Id, null, badge, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        var unlocked = await _gamification.GetUserBadgesAsync(userId, cancellationToken);
        return MapBadge(badge, criteria, unlocked);
    }

    public async Task<BadgeResponseDto> UpdateBadgeAsync(string userId, Guid badgeId, BadgeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateBadgeRequest(request);
        await ValidateBadgeCriteriaAsync(userId, request.Criteria, cancellationToken);
        var badge = (await _gamification.GetBadgesAsync(userId, cancellationToken)).FirstOrDefault(x => x.Id == badgeId) ?? throw new KeyNotFoundException("Badge was not found.");
        var existing = await _gamification.GetBadgeCriteriaAsync([badgeId], cancellationToken);
        foreach (var criterion in existing) await _gamification.RemoveAsync(criterion, cancellationToken);
        var criteria = request.Criteria.Select(x => MapCriterion(badge.Id, x)).ToArray();
        await _gamification.AddRangeAsync(criteria, cancellationToken);
        badge.Name = request.Name.Trim(); badge.Description = request.Description.Trim(); badge.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Updated, "Badge", badge.Id, null, badge, badge.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
        return MapBadge(badge, criteria, await _gamification.GetUserBadgesAsync(userId, cancellationToken));
    }

    public async Task ArchiveBadgeAsync(string userId, Guid badgeId, CancellationToken cancellationToken = default)
    {
        var badge = (await _gamification.GetBadgesAsync(userId, cancellationToken)).FirstOrDefault(x => x.Id == badgeId) ?? throw new KeyNotFoundException("Badge was not found.");
        badge.Archived = true; badge.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Archived, "Badge", badge.Id, null, badge, badge.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RefreshAsync(userId, cancellationToken);
    }

    public async Task RefreshAsync(string userId, CancellationToken cancellationToken = default)
    {
        var transactions = await _gamification.GetTransactionsAsync(userId, cancellationToken);
        var categories = await _gamification.GetCategoriesAsync(userId, cancellationToken);
        var entries = await _gamification.GetXpEntriesAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;
        await SyncPositiveMonthsAsync(userId, transactions, categories, entries, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var goals = await _gamification.GetGoalsAsync(userId, cancellationToken);
        var sources = await _gamification.GetGoalSourcesAsync(goals.Select(x => x.Id).ToArray(), cancellationToken);
        await SyncGoalsAsync(userId, goals, sources, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncBadgesAsync(userId, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncPositiveMonthsAsync(string userId, IReadOnlyCollection<FinancialTransaction> transactions, IReadOnlyCollection<FinancialCategory> categories, IReadOnlyCollection<XpLedgerEntry> entries, DateTime now, CancellationToken cancellationToken)
    {
        var today = LocalToday();
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var months = transactions.Where(x => x.TransactionDate < currentMonth).Select(x => FirstDayOfMonth(x.TransactionDate)).Distinct().ToArray();
        var budgets = await _gamification.GetBudgetsAsync(categories.Where(x => x.Type == FinancialCategoryType.Expense).Select(x => x.Id).ToArray(), cancellationToken);
        foreach (var month in months)
        {
            var applicableBudgets = budgets
                .GroupBy(x => x.CategoryId)
                .Select(x => x.Where(y => y.EffectiveFrom <= month).OrderByDescending(y => y.EffectiveFrom).FirstOrDefault())
                .Where(x => x is not null)
                .Cast<CategoryBudget>()
                .ToArray();
            var budgetOverrides = await _gamification.GetBudgetOverridesAsync(applicableBudgets.Select(x => x.Id).ToArray(), month, cancellationToken);
            var confirmed = transactions.Where(x => x.DeletedAt is null && x.Status == TransactionStatus.Confirmed && FirstDayOfMonth(x.TransactionDate) == month).ToArray();
            var income = confirmed.Where(x => x.Type == FinancialCategoryType.Income).Sum(x => x.Amount);
            var expense = confirmed.Where(x => x.Type == FinancialCategoryType.Expense).Sum(x => x.Amount);
            var withinBudget = applicableBudgets.All(budget =>
            {
                var limit = budgetOverrides.FirstOrDefault(x => x.CategoryBudgetId == budget.Id)?.Amount ?? budget.Amount;
                var spent = confirmed.Where(x => x.CategoryId == budget.CategoryId && x.Type == FinancialCategoryType.Expense).Sum(x => x.Amount);
                return spent <= limit;
            });
            await SyncEventAsync(userId, XpEventType.PositiveMonth, PositiveMonthSourceType, StableId($"{userId}:{month:yyyy-MM-dd}"), income > expense && withinBudget, entries, now, cancellationToken);
        }
    }

    private async Task SyncGoalsAsync(string userId, IReadOnlyCollection<Goal> goals, IReadOnlyCollection<GoalSourceLink> sources, DateTime now, CancellationToken cancellationToken)
    {
        var entries = await _gamification.GetXpEntriesAsync(userId, cancellationToken);
        foreach (var goal in goals.Where(x => !x.Archived && x.Status != GoalStatus.Cancelled))
        {
            var progress = await CalculateGoalProgressAsync(userId, goal, sources, cancellationToken);
            var qualifies = progress >= goal.TargetValue;
            goal.Status = qualifies ? GoalStatus.Completed : GoalStatus.Active;
            goal.UpdatedAt = now;
            await SyncEventAsync(userId, XpEventType.GoalCompleted, GoalSourceTypeName, goal.Id, qualifies, entries, now, cancellationToken);
        }
    }

    private async Task SyncEventAsync(string userId, XpEventType eventType, string sourceType, Guid sourceId, bool qualifies, IReadOnlyCollection<XpLedgerEntry> entries, DateTime now, CancellationToken cancellationToken)
    {
        var sourceEntries = entries.Where(x => x.SourceType == sourceType && x.SourceId == sourceId).ToArray();
        var grant = ActiveGrant(sourceEntries);
        if (qualifies && grant is null)
        {
            var rule = (await _gamification.GetXpRulesAsync(userId, cancellationToken)).FirstOrDefault(x => x.EventType == eventType);
            if (rule is not null) await _gamification.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Grant, Amount = rule.Amount, EventType = eventType, SourceType = sourceType, SourceId = sourceId, CreatedAt = now }, cancellationToken);
        }
        else if (!qualifies && grant is not null)
        {
            await _gamification.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Reversal, Amount = -grant.Amount, EventType = eventType, SourceType = sourceType, SourceId = sourceId, ReversedEntryId = grant.Id, CreatedAt = now }, cancellationToken);
        }
    }

    private async Task SyncBadgesAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var badges = (await _gamification.GetBadgesAsync(userId, cancellationToken)).Where(x => !x.Archived).ToArray();
        var criteria = await _gamification.GetBadgeCriteriaAsync(badges.Select(x => x.Id).ToArray(), cancellationToken);
        var unlocked = await _gamification.GetUserBadgesAsync(userId, cancellationToken);
        var statistics = await GetStatisticsAsync(userId, cancellationToken);
        foreach (var badge in badges)
        {
            var badgeCriteria = criteria.Where(x => x.BadgeId == badge.Id).ToArray();
            var meets = badgeCriteria.Length > 0 && badgeCriteria.All(x => MeetsCriterion(x, statistics));
            var existing = unlocked.FirstOrDefault(x => x.BadgeId == badge.Id);
            if (meets && existing is null) await _gamification.AddAsync(new UserBadge { UserId = userId, BadgeId = badge.Id, UnlockedAt = now }, cancellationToken);
            if (!meets && existing is not null) await _gamification.RemoveAsync(existing, cancellationToken);
        }
    }

    private async Task<decimal> CalculateGoalProgressAsync(string userId, Goal goal, IReadOnlyCollection<GoalSourceLink> sources, CancellationToken cancellationToken)
    {
        if (goal.Type == GoalType.FreeForm) return goal.ManualProgress ?? 0;
        if (goal.Type == GoalType.Financial) return await PositiveMonthStreakAsync(userId, cancellationToken);
        var source = sources.FirstOrDefault(x => x.GoalId == goal.Id);
        if (source is null) return 0;
        if (goal.Type == GoalType.Habit)
        {
            var schedule = (await _gamification.GetHabitSchedulesAsync([source.SourceId], cancellationToken)).FirstOrDefault();
            if (schedule is null) return 0;
            var weekdays = await _gamification.GetHabitWeekdaysAsync([schedule.Id], cancellationToken);
            var completions = (await _gamification.GetHabitCompletionsAsync(userId, cancellationToken))
                .Where(x => x.HabitId == source.SourceId && x.DeletedAt is null).ToArray();
            return HabitStreak(schedule, weekdays, completions);
        }
        var sessions = (await _gamification.GetWorkoutSessionsAsync(userId, cancellationToken))
            .Where(x => x.Status == WorkoutSessionStatus.Completed && x.DeletedAt is null && x.CompletedAt.HasValue).ToArray();
        if (source.SourceType == GoalSourceType.WorkoutSheet) sessions = sessions.Where(x => x.WorkoutSheetId == source.SourceId).ToArray();
        if (source.SourceType == GoalSourceType.Exercise)
        {
            var exercises = await _gamification.GetWorkoutSessionExercisesAsync(sessions.Select(x => x.Id).ToArray(), cancellationToken);
            var sessionIds = exercises.Where(x => x.ExerciseId == source.SourceId).Select(x => x.WorkoutSessionId).ToHashSet();
            sessions = sessions.Where(x => sessionIds.Contains(x.Id)).ToArray();
        }
        return WorkoutStreak(sessions);
    }

    private async Task<int> PositiveMonthStreakAsync(string userId, CancellationToken cancellationToken)
    {
        var entries = await _gamification.GetXpEntriesAsync(userId, cancellationToken);
        var active = entries.Where(x => x.EventType == XpEventType.PositiveMonth && x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id)).Select(x => x.SourceId).ToHashSet();
        var month = FirstDayOfMonth(LocalToday()).AddMonths(-1);
        var count = 0;
        while (active.Contains(StableId($"{userId}:{month:yyyy-MM-dd}"))) { count++; month = month.AddMonths(-1); }
        return count;
    }

    private static int HabitStreak(HabitSchedule schedule, IReadOnlyCollection<HabitScheduleWeekday> weekdays, IReadOnlyCollection<HabitCompletion> completions)
    {
        var count = 0;
        var current = schedule.Type == HabitScheduleType.WeeklyCount ? WeekStart(LocalToday()) : LocalToday();
        while (true)
        {
            if (schedule.Type == HabitScheduleType.Weekdays && !weekdays.Any(x => x.DayOfWeek == current.DayOfWeek)) { current = current.AddDays(-1); continue; }
            var start = schedule.Type == HabitScheduleType.WeeklyCount ? current : current;
            var end = schedule.Type == HabitScheduleType.WeeklyCount ? current.AddDays(6) : current;
            if (completions.Count(x => x.CompletedOn >= start && x.CompletedOn <= end) < schedule.TargetCount) return count;
            count++;
            current = schedule.Type == HabitScheduleType.WeeklyCount ? current.AddDays(-7) : current.AddDays(-1);
        }
    }

    private static int WorkoutStreak(IReadOnlyCollection<WorkoutSession> sessions)
    {
        var count = 0;
        var week = WeekStart(LocalToday());
        while (sessions.Any(x => WeekStart(DateOnly.FromDateTime(x.CompletedAt!.Value)) == week)) { count++; week = week.AddDays(-7); }
        return count;
    }

    private sealed class Statistics
    {
        public int TotalXp { get; init; }
        public int Level { get; init; }
        public int HabitCompletions { get; init; }
        public int WeeklyHabitGoals { get; init; }
        public int WorkoutCompletions { get; init; }
        public int TransactionConfirmations { get; init; }
        public int GoalCompletions { get; init; }
        public int PositiveMonths { get; init; }
        public IReadOnlyCollection<HabitCompletion> Completions { get; init; } = [];
        public IReadOnlyCollection<FinancialTransaction> Transactions { get; init; } = [];
        public IReadOnlyCollection<Goal> Goals { get; init; } = [];
        public IReadOnlyCollection<WorkoutSession> Sessions { get; init; } = [];
        public IReadOnlyCollection<WorkoutSessionExercise> SessionExercises { get; init; } = [];
    }

    private async Task<Statistics> GetStatisticsAsync(string userId, CancellationToken cancellationToken)
    {
        var entries = await _gamification.GetXpEntriesAsync(userId, cancellationToken);
        var completions = await _gamification.GetHabitCompletionsAsync(userId, cancellationToken);
        var transactions = await _gamification.GetTransactionsAsync(userId, cancellationToken);
        var goals = await _gamification.GetGoalsAsync(userId, cancellationToken);
        var sessions = await _gamification.GetWorkoutSessionsAsync(userId, cancellationToken);
        var sessionExercises = await _gamification.GetWorkoutSessionExercisesAsync(sessions.Select(x => x.Id).ToArray(), cancellationToken);
        var active = entries.Where(x => x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id)).ToArray();
        var level = CalculateLevel(entries.Sum(x => x.Amount), await _gamification.GetLevelRuleAsync(userId, cancellationToken));
        return new()
        {
            TotalXp = entries.Sum(x => x.Amount),
            Level = level,
            HabitCompletions = active.Count(x => x.EventType == XpEventType.HabitCompletion),
            WeeklyHabitGoals = active.Count(x => x.EventType == XpEventType.WeeklyHabitGoal),
            WorkoutCompletions = active.Count(x => x.EventType == XpEventType.WorkoutCompleted),
            TransactionConfirmations = active.Count(x => x.EventType == XpEventType.TransactionConfirmed),
            GoalCompletions = active.Count(x => x.EventType == XpEventType.GoalCompleted),
            PositiveMonths = active.Count(x => x.EventType == XpEventType.PositiveMonth),
            Completions = completions,
            Transactions = transactions,
            Goals = goals,
            Sessions = sessions,
            SessionExercises = sessionExercises
        };
    }

    private static bool MeetsCriterion(BadgeCriterion criterion, Statistics statistics)
    {
        return criterion.Type switch
        {
            BadgeCriterionType.Xp => statistics.TotalXp >= criterion.TargetValue,
            BadgeCriterionType.Level => statistics.Level >= criterion.TargetValue,
            BadgeCriterionType.HabitCompletionCount =>
                HabitCompletionCount(criterion, statistics) >= criterion.TargetValue,
            BadgeCriterionType.WeeklyHabitGoalCount =>
                statistics.WeeklyHabitGoals >= criterion.TargetValue,
            BadgeCriterionType.WorkoutCompletionCount =>
                WorkoutCompletionCount(criterion, statistics) >= criterion.TargetValue,
            BadgeCriterionType.TransactionConfirmationCount =>
                TransactionConfirmationCount(criterion, statistics) >= criterion.TargetValue,
            BadgeCriterionType.GoalCompletionCount =>
                GoalCompletionCount(criterion, statistics) >= criterion.TargetValue,
            BadgeCriterionType.PositiveMonthCount =>
                statistics.PositiveMonths >= criterion.TargetValue,
            _ => false
        };
    }

    private async Task ValidateGoalSourcesAsync(
        string userId,
        GoalType type,
        IReadOnlyCollection<GoalSourceRequestDto> sources,
        CancellationToken cancellationToken)
    {
        var valid = type switch
        {
            GoalType.Financial => sources.Count == 0,
            GoalType.Habit => sources.Count == 1 && sources.Single().SourceType == GoalSourceType.Habit,
            GoalType.Training => sources.Count == 1 && sources.Single().SourceType is GoalSourceType.Exercise or GoalSourceType.WorkoutSheet,
            GoalType.FreeForm => sources.Count == 0,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException("Goal sources are invalid for its type.");
        }

        if (type == GoalType.Habit &&
            !(await _gamification.GetHabitsAsync(userId, cancellationToken))
                .Any(x => x.Id == sources.Single().SourceId))
        {
            throw new KeyNotFoundException("Habit source was not found.");
        }

        if (type == GoalType.Training &&
            sources.Single().SourceType == GoalSourceType.WorkoutSheet &&
            !(await _gamification.GetWorkoutSheetsAsync(userId, cancellationToken))
                .Any(x => x.Id == sources.Single().SourceId))
        {
            throw new KeyNotFoundException("Workout sheet source was not found.");
        }

        if (type == GoalType.Training &&
            sources.Single().SourceType == GoalSourceType.Exercise &&
            !(await _gamification.GetExercisesAsync(userId, cancellationToken))
                .Any(x => x.Id == sources.Single().SourceId))
        {
            throw new KeyNotFoundException("Exercise source was not found.");
        }
    }

    private async Task ValidateBadgeCriteriaAsync(
        string userId,
        IReadOnlyCollection<BadgeCriterionRequestDto> criteria,
        CancellationToken cancellationToken)
    {
        var habits = await _gamification.GetHabitsAsync(userId, cancellationToken);
        var goals = await _gamification.GetGoalsAsync(userId, cancellationToken);
        var exercises = await _gamification.GetExercisesAsync(userId, cancellationToken);
        var categories = await _gamification.GetCategoriesAsync(userId, cancellationToken);
        var hasMissingResource = criteria.Any(x =>
            x.HabitId.HasValue && !habits.Any(y => y.Id == x.HabitId) ||
            x.GoalId.HasValue && !goals.Any(y => y.Id == x.GoalId) ||
            x.ExerciseId.HasValue && !exercises.Any(y => y.Id == x.ExerciseId) ||
            x.FinancialCategoryId.HasValue && !categories.Any(y => y.Id == x.FinancialCategoryId));

        if (hasMissingResource)
        {
            throw new KeyNotFoundException("Badge criterion resource was not found.");
        }
    }

    private static int CalculateLevel(int totalXp, LevelProgressionRule? rule)
    {
        if (rule is null || totalXp < rule.BaseXp)
        {
            return 1;
        }

        var level = 1;
        while (totalXp >= LevelThreshold(level + 1, rule))
        {
            level++;
        }
        return level;
    }

    private static int LevelThreshold(int level, LevelProgressionRule? rule)
    {
        if (rule is null || level <= 1)
        {
            return 0;
        }
        var advances = level - 1;
        return advances * rule.BaseXp + advances * (advances - 1) / 2 * rule.IncrementPerLevel;
    }

    private static int HabitCompletionCount(BadgeCriterion criterion, Statistics statistics)
    {
        return criterion.HabitId.HasValue
            ? statistics.Completions.Count(x => x.HabitId == criterion.HabitId && x.DeletedAt is null)
            : statistics.HabitCompletions;
    }

    private static int WorkoutCompletionCount(BadgeCriterion criterion, Statistics statistics)
    {
        if (!criterion.ExerciseId.HasValue)
        {
            return statistics.WorkoutCompletions;
        }

        return statistics.Sessions.Count(x =>
            x.Status == WorkoutSessionStatus.Completed &&
            x.DeletedAt is null &&
            statistics.SessionExercises.Any(y =>
                y.WorkoutSessionId == x.Id &&
                y.ExerciseId == criterion.ExerciseId));
    }

    private static int TransactionConfirmationCount(BadgeCriterion criterion, Statistics statistics)
    {
        return criterion.FinancialCategoryId.HasValue
            ? statistics.Transactions.Count(x =>
                x.CategoryId == criterion.FinancialCategoryId &&
                x.Status == TransactionStatus.Confirmed &&
                x.DeletedAt is null)
            : statistics.TransactionConfirmations;
    }

    private static int GoalCompletionCount(BadgeCriterion criterion, Statistics statistics)
    {
        return criterion.GoalId.HasValue
            ? statistics.Goals.Count(x =>
                x.Id == criterion.GoalId &&
                x.Status == GoalStatus.Completed &&
                !x.Archived)
            : statistics.GoalCompletions;
    }

    private static XpLedgerEntry? ActiveGrant(IEnumerable<XpLedgerEntry> entries)
    {
        return entries.FirstOrDefault(x =>
            x.Type == XpLedgerEntryType.Grant &&
            !entries.Any(y =>
                y.Type == XpLedgerEntryType.Reversal &&
                y.ReversedEntryId == x.Id));
    }

    private static Guid StableId(string value)
    {
        return new Guid(MD5.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static DateOnly FirstDayOfMonth(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        return date.AddDays(-((int)date.DayOfWeek + 6) % 7);
    }

    private static DateOnly LocalToday()
    {
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
    }

    private async Task<Goal> RequiredGoalAsync(
        string userId,
        Guid goalId,
        CancellationToken cancellationToken)
    {
        return await _gamification.GetGoalAsync(userId, goalId, cancellationToken)
            ?? throw new KeyNotFoundException("Goal was not found.");
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentException("Pagination is invalid.");
        }
    }

    private static void ValidateGoalRequest(GoalRequestDto request)
    {
        if (!Enum.IsDefined(request.Type) ||
            string.IsNullOrWhiteSpace(request.Title) ||
            request.Title.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(request.Unit) ||
            request.Unit.Trim().Length > 60 ||
            request.TargetValue <= 0)
        {
            throw new ArgumentException("Goal is invalid.");
        }
    }

    private static void ValidateBadgeRequest(BadgeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length > 300 ||
            request.Criteria.Count == 0 ||
            request.Criteria.Any(x => !Enum.IsDefined(x.Type) || x.TargetValue <= 0))
        {
            throw new ArgumentException("Badge is invalid.");
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static GoalResponseDto MapGoal(
        Goal goal,
        IReadOnlyCollection<GoalSourceLink> sources,
        decimal progress)
    {
        return new()
        {
            Id = goal.Id,
            Type = goal.Type,
            Status = goal.Status,
            Title = goal.Title,
            Description = goal.Description,
            TargetValue = goal.TargetValue,
            Progress = progress,
            Unit = goal.Unit,
            DueDate = goal.DueDate,
            Archived = goal.Archived,
            Sources = sources.Where(x => x.GoalId == goal.Id)
                .Select(x => new GoalSourceRequestDto
                {
                    SourceType = x.SourceType,
                    SourceId = x.SourceId
                })
                .ToArray()
        };
    }

    private static BadgeResponseDto MapBadge(
        Badge badge,
        IReadOnlyCollection<BadgeCriterion> criteria,
        IReadOnlyCollection<UserBadge> unlocked)
    {
        return new()
        {
            Id = badge.Id,
            Name = badge.Name,
            Description = badge.Description,
            Archived = badge.Archived,
            UnlockedAt = unlocked.FirstOrDefault(x => x.BadgeId == badge.Id)?.UnlockedAt,
            Criteria = criteria.Where(x => x.BadgeId == badge.Id)
                .Select(x => new BadgeCriterionRequestDto
                {
                    Type = x.Type,
                    TargetValue = x.TargetValue,
                    HabitId = x.HabitId,
                    ExerciseId = x.ExerciseId,
                    FinancialCategoryId = x.FinancialCategoryId,
                    GoalId = x.GoalId
                })
                .ToArray()
        };
    }

    private static XpEventRuleRequestDto MapRule(XpEventRule rule)
    {
        return new() { EventType = rule.EventType, Amount = rule.Amount };
    }

    private static XpLedgerEntryResponseDto MapLedger(XpLedgerEntry entry)
    {
        return new()
        {
            Id = entry.Id,
            Type = entry.Type,
            Amount = entry.Amount,
            EventType = entry.EventType,
            SourceType = entry.SourceType,
            SourceId = entry.SourceId,
            ReversedEntryId = entry.ReversedEntryId,
            CreatedAt = entry.CreatedAt
        };
    }

    private static BadgeCriterion MapCriterion(Guid badgeId, BadgeCriterionRequestDto value)
    {
        return new()
        {
            BadgeId = badgeId,
            Type = value.Type,
            TargetValue = value.TargetValue,
            HabitId = value.HabitId,
            ExerciseId = value.ExerciseId,
            FinancialCategoryId = value.FinancialCategoryId,
            GoalId = value.GoalId
        };
    }

    private Task AuditAsync(
        string userId,
        AuditAction action,
        string resourceType,
        Guid? resourceId,
        object? previous,
        object? current,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (_auditLogs is null)
        {
            return Task.CompletedTask;
        }

        return _auditLogs.CreateAsync(
            new AuditLog
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous),
                CurrentValues = current is null ? null : JsonSerializer.Serialize(current),
                CreatedAt = now
            },
            cancellationToken);
    }
}
