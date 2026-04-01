using CookDating.Bff.Infrastructure;
using CookDating.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CookDating.UnitTests.Infrastructure;

[TestFixture]
public class TenantContextMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_SetsTenantIdFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TENANT_ID"] = "tech-dating" })
            .Build();

        var tenantContext = new TenantContext();
        var nextCalled = false;
        var middleware = new TenantContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, config);

        await middleware.InvokeAsync(new DefaultHttpContext(), tenantContext);

        Assert.Multiple(() =>
        {
            Assert.That(tenantContext.TenantId, Is.EqualTo("tech-dating"));
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public async Task InvokeAsync_DefaultsToCookDating_WhenNoConfig()
    {
        var config = new ConfigurationBuilder().Build();
        var tenantContext = new TenantContext();
        var middleware = new TenantContextMiddleware(_ => Task.CompletedTask, config);

        await middleware.InvokeAsync(new DefaultHttpContext(), tenantContext);

        Assert.That(tenantContext.TenantId, Is.EqualTo("cook-dating"));
    }

    [Test]
    public async Task InvokeAsync_DoesNotThrow_WhenTenantContextIsNotMutable()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TENANT_ID"] = "test" })
            .Build();

        var readOnlyContext = new ReadOnlyTenantContext("original");
        var middleware = new TenantContextMiddleware(_ => Task.CompletedTask, config);

        await middleware.InvokeAsync(new DefaultHttpContext(), readOnlyContext);

        Assert.That(readOnlyContext.TenantId, Is.EqualTo("original"));
    }

    private sealed class ReadOnlyTenantContext(string tenantId) : ITenantContext
    {
        public string TenantId { get; } = tenantId;
    }
}
