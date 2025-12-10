using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BurhaniGuards.Api.Middleware;

public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
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

        // Validate token format (64 character hex string for SHA256)
        // Accept any non-empty token for now - in production you'd validate against a token store
        if (!string.IsNullOrWhiteSpace(token))
        {
            // Token is present - create a claims identity
            // Store token in claims - middleware will use it to look up user
            var claims = new[] 
            { 
                new Claim(ClaimTypes.NameIdentifier, token),
                new Claim(ClaimTypes.AuthenticationMethod, "Bearer")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        return Task.CompletedTask;
    }
}

