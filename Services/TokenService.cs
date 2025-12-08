using System.Security.Cryptography;
using System.Text;

namespace BurhaniGuards.Api.Services;

public sealed class TokenService : ITokenService
{
    public string GenerateToken(string subject, string role)
    {
        var payload = $"{subject}:{role}:{Guid.NewGuid():N}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}

