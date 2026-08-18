namespace ServiceLifeOS.Domain.Entities;

public sealed class UserPreference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public WeightUnit PreferredWeightUnit { get; set; } = WeightUnit.Kilograms;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string TokenId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserSessionId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByRefreshTokenId { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string? PreviousValues { get; set; }
    public string? CurrentValues { get; set; }
    public DateTime CreatedAt { get; set; }
}
