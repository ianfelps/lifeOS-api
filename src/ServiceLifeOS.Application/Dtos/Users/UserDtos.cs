using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Users;

public sealed class ChangePasswordRequestDto
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }
}

public sealed class UpdateUserIdentityRequestDto
{
    public string? UserName { get; set; }

    public string? DisplayName { get; set; }
}

public sealed class UserIdentityResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UserPreferenceResponseDto
{
    public WeightUnit PreferredWeightUnit { get; set; }
}

public sealed class UpdateUserPreferenceRequestDto
{
    public WeightUnit PreferredWeightUnit { get; set; }
}

public sealed class RevokeOtherSessionsResponseDto
{
    public int RevokedSessionCount { get; set; }
}
