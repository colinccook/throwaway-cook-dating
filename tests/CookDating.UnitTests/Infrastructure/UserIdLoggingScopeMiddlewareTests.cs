using System.Security.Claims;
using CookDating.Bff.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CookDating.UnitTests.Infrastructure;

[TestFixture]
public class UserIdLoggingScopeMiddlewareTests
{
    private ScopeCapturingLogger<UserIdLoggingScopeMiddleware> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new ScopeCapturingLogger<UserIdLoggingScopeMiddleware>();
    }

    [Test]
    public async Task InvokeAsync_WithAuthenticatedUser_AddsUserIdToLogScope()
    {
        var userId = "user-abc-123";
        var context = CreateHttpContext(new Claim("sub", userId));
        var middleware = new UserIdLoggingScopeMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(context);

        Assert.That(_logger.CapturedScopes, Has.Count.EqualTo(1));
        var scope = _logger.CapturedScopes[0] as IReadOnlyDictionary<string, object>;
        Assert.That(scope, Is.Not.Null);
        Assert.That(scope!["UserId"], Is.EqualTo(userId));
    }

    [Test]
    public async Task InvokeAsync_WithUnauthenticatedUser_DoesNotAddScope()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new UserIdLoggingScopeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _logger);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        Assert.That(_logger.CapturedScopes, Is.Empty);
    }

    [Test]
    public async Task InvokeAsync_WithUserMissingSubClaim_DoesNotAddScope()
    {
        var context = CreateHttpContext(new Claim("email", "test@example.com"));
        var nextCalled = false;
        var middleware = new UserIdLoggingScopeMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _logger);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
        Assert.That(_logger.CapturedScopes, Is.Empty);
    }

    [Test]
    public async Task InvokeAsync_WithAuthenticatedUser_InvokesNextInsideScope()
    {
        var userId = "user-456";
        var context = CreateHttpContext(new Claim("sub", userId));
        var scopeActiveWhenNextCalled = false;

        var middleware = new UserIdLoggingScopeMiddleware(
            _ =>
            {
                scopeActiveWhenNextCalled = _logger.ActiveScopeCount > 0;
                return Task.CompletedTask;
            },
            _logger);

        await middleware.InvokeAsync(context);

        Assert.That(scopeActiveWhenNextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_ScopeIsDisposedAfterNextCompletes()
    {
        var context = CreateHttpContext(new Claim("sub", "user-789"));
        var middleware = new UserIdLoggingScopeMiddleware(_ => Task.CompletedTask, _logger);

        await middleware.InvokeAsync(context);

        Assert.That(_logger.ActiveScopeCount, Is.Zero);
    }

    private static DefaultHttpContext CreateHttpContext(params Claim[] claims)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test-auth"));
        return context;
    }
}

/// <summary>
/// A minimal ILogger that captures BeginScope calls and tracks active scope lifetime.
/// </summary>
internal class ScopeCapturingLogger<T> : ILogger<T>
{
    public List<object?> CapturedScopes { get; } = [];
    public int ActiveScopeCount { get; private set; }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        CapturedScopes.Add(state);
        ActiveScopeCount++;
        return new ScopeTracker(this);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class ScopeTracker(ScopeCapturingLogger<T> logger) : IDisposable
    {
        public void Dispose() => logger.ActiveScopeCount--;
    }
}
