using BurhaniGuards.Api.Services;

namespace BurhaniGuards.Api.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenStore tokenStore)
    {
        // Only process if user is authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var token = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    // Look up user from token store
                    var currentUser = tokenStore.GetUserByToken(token);
                    
                    if (currentUser != null)
                    {
                        // Set CurrentUser in HttpContext.Items
                        context.Items["User"] = currentUser;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    var logger = context.RequestServices.GetRequiredService<ILogger<UserContextMiddleware>>();
                    logger.LogError(ex, "Error setting user context: {Error}", ex.Message);
                }
            }
        }

        await _next(context);
    }
}

