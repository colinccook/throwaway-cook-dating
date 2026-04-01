namespace CookDating.SharedKernel.Infrastructure;

public interface ITenantContext
{
    string TenantId { get; }
}
