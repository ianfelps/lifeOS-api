using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Infrastructure.Options;

namespace ServiceLifeOS.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenData CreateAccessToken(string userId, string userName, string displayName)
    {
        var tokenId = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, displayName),
            new("username", userName),
            new(JwtRegisteredClaimNames.Jti, tokenId)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new()
        {
            Value = new JwtSecurityTokenHandler().WriteToken(token),
            TokenId = tokenId,
            ExpiresAt = expires
        };
    }

    public RefreshTokenData CreateRefreshToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new()
        {
            Value = value,
            Hash = HashRefreshToken(value),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays)
        };
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
