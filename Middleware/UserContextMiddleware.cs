using BurhaniGuards.Api.ViewModel;
using BurhaniGuards.Api.Services;

namespace BurhaniGuards.Api.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process if user is authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var idClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var fullName = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var itsId = context.User.FindFirst("its_id")?.Value;
                var rank = context.User.FindFirst("rank")?.Value;
                var rolesClaim = context.User.FindFirst("roles")?.Value;
                var jamiyat = context.User.FindFirst("jamiyat")?.Value;
                var jamaat = context.User.FindFirst("jamaat")?.Value;
                var requiresPasswordChangeClaim = context.User.FindFirst("requires_password_change")?.Value;

                int.TryParse(idClaim, out var userId);
                int? roles = int.TryParse(rolesClaim, out var parsedRoles)
                    ? parsedRoles
                    : null;
                bool.TryParse(requiresPasswordChangeClaim, out var requiresPasswordChange);

                if (userId > 0)
                {
                    context.Items["User"] = new CurrentUserViewModel
                    {
                        id = userId,
                        itsId = itsId,
                        fullName = fullName ?? string.Empty,
                        email = email ?? string.Empty,
                        rank = rank ?? string.Empty,
                        roles = roles,
                        jamiyat = jamiyat,
                        jamaat = jamaat,
                        requiresPasswordChange = requiresPasswordChange
                    };
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                var logger = context.RequestServices.GetRequiredService<ILogger<UserContextMiddleware>>();
                logger.LogError(ex, "Error setting user context: {Error}", ex.Message);
            }
        }

        await _next(context);
    }
}

