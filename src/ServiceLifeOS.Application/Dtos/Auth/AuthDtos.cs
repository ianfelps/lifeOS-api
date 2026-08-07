namespace ServiceLifeOS.Dtos.Auth;

public sealed class LoginRequestDto
{
    public string? UserName { get; set; }

    public string? Password { get; set; }
}

public sealed class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public MeResponseDto User { get; set; } = new();
}

public sealed class MeResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
