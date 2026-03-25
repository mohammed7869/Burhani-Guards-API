using BurhaniGuards.Api.ViewModel;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BurhaniGuards.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly byte[] _secretKeyBytes;
    private readonly TimeSpan _tokenLifetime;

    public TokenService(IConfiguration configuration)
    {
        var tokenSecret = configuration["Auth:TokenSecret"];
        if (string.IsNullOrWhiteSpace(tokenSecret))
        {
            throw new InvalidOperationException("Auth:TokenSecret configuration is missing.");
        }

        var tokenLifetimeDays = configuration.GetValue("Auth:TokenLifetimeDays", 30);
        _secretKeyBytes = Encoding.UTF8.GetBytes(tokenSecret);
        _tokenLifetime = TimeSpan.FromDays(tokenLifetimeDays <= 0 ? 30 : tokenLifetimeDays);
    }

    public string GenerateToken(string subject, string role)
    {
        var payload = new AuthTokenData
        {
            ItsId = subject.Contains('@') ? null : subject,
            Email = subject.Contains('@') ? subject : null,
            FullName = string.Empty,
            Role = role,
            IssuedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ExpiresAtUnixSeconds = DateTimeOffset.UtcNow.Add(_tokenLifetime).ToUnixTimeSeconds()
        };

        return SignPayload(payload);
    }

    public string GenerateToken(CurrentUserViewModel user, string role)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new AuthTokenData
        {
            Id = user.id,
            ItsId = user.itsId,
            Email = user.email,
            FullName = user.fullName,
            Rank = user.rank,
            Roles = user.roles,
            Jamiyat = user.jamiyat,
            Jamaat = user.jamaat,
            Role = role,
            RequiresPasswordChange = user.requiresPasswordChange,
            IssuedAtUnixSeconds = now.ToUnixTimeSeconds(),
            ExpiresAtUnixSeconds = now.Add(_tokenLifetime).ToUnixTimeSeconds()
        };

        return SignPayload(payload);
    }

    public AuthTokenData? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        var payloadSegment = parts[0];
        var signatureSegment = parts[1];
        var expectedSignature = ComputeSignature(payloadSegment);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signatureSegment),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return null;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(payloadSegment);
            var payload = JsonSerializer.Deserialize<AuthTokenData>(payloadBytes);
            if (payload == null)
            {
                return null;
            }

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.ExpiresAtUnixSeconds <= nowUnix)
            {
                return null;
            }

            return payload;
        }
        catch
        {
            return null;
        }
    }

    private string SignPayload(AuthTokenData payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signatureSegment = ComputeSignature(payloadSegment);
        return $"{payloadSegment}.{signatureSegment}";
    }

    private string ComputeSignature(string payloadSegment)
    {
        using var hmac = new HMACSHA256(_secretKeyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
        return Base64UrlEncode(signatureBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
}

