using CookDating.SharedKernel.Infrastructure;

namespace CookDating.Bff.Infrastructure;

/// <summary>
/// Sets the scoped TenantId on every request from the BFF's configured tenant.
/// </summary>
public class TenantContextMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _tenantId = configuration["TENANT_ID"] ?? "cook-dating";

    public Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (tenantContext is TenantContext mutable)
        {
            mutable.TenantId = _tenantId;
        }
        return next(context);
    }
}
