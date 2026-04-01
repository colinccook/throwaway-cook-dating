namespace CookDating.SharedKernel.Infrastructure;

public class TenantContext : ITenantContext
{
    public string TenantId { get; set; } = string.Empty;
}
