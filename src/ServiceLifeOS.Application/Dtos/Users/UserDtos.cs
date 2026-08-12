using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Users;

public sealed class ChangePasswordRequestDto
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }
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
