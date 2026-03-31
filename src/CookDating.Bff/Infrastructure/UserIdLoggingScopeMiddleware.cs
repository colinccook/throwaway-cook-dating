namespace CookDating.Bff.Infrastructure;

/// <summary>
/// Enriches the logging scope with the authenticated user's ID so that
/// every log entry within an HTTP request includes the UserId property,
/// making it easy to filter and trace per-user activity in the Aspire dashboard.
/// </summary>
public class UserIdLoggingScopeMiddleware(RequestDelegate next, ILogger<UserIdLoggingScopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (userId is not null)
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["UserId"] = userId }))
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }
}
