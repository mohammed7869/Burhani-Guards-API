using BurhaniGuards.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BurhaniGuards.Api.Middleware;

public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenService _tokenService;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITokenService tokenService)
        : base(options, logger, encoder)
    {
        _tokenService = tokenService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var tokenData = _tokenService.ValidateToken(token);
        if (tokenData == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.AuthenticationMethod, "Bearer"),
            new("role_slug", tokenData.Role),
            new("rank", tokenData.Rank),
            new("requires_password_change", tokenData.RequiresPasswordChange.ToString().ToLowerInvariant()),
            new("issued_at", tokenData.IssuedAtUnixSeconds.ToString()),
            new("expires_at", tokenData.ExpiresAtUnixSeconds.ToString())
        };

        if (tokenData.Id > 0)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, tokenData.Id.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(tokenData.FullName))
        {
            claims.Add(new Claim(ClaimTypes.Name, tokenData.FullName));
        }

        if (!string.IsNullOrWhiteSpace(tokenData.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, tokenData.Email));
        }

        if (!string.IsNullOrWhiteSpace(tokenData.ItsId))
        {
            claims.Add(new Claim("its_id", tokenData.ItsId));
        }

        if (tokenData.Roles.HasValue)
        {
            claims.Add(new Claim("roles", tokenData.Roles.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(tokenData.Jamiyat))
        {
            claims.Add(new Claim("jamiyat", tokenData.Jamiyat));
        }

        if (!string.IsNullOrWhiteSpace(tokenData.Jamaat))
        {
            claims.Add(new Claim("jamaat", tokenData.Jamaat));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        return Task.CompletedTask;
    }
}

