namespace BurhaniGuards.Api.Services;

public interface ITokenService
{
    string GenerateToken(string subject, string role);
}

