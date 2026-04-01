using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using CookDating.BddTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.Hooks;

[Binding]
public sealed class AspireHook
{
    private static DistributedApplication? _app;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static LogCollector? _logCollector;
    private static string[] _tenants = [];

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _tenants = ["cook-dating", "tech-dating"];

        // Start the Aspire app with two tenants
        var args = _tenants
            .SelectMany((t, i) => new[] { $"--Tenants:{i}={t}" })
            .ToArray();

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CookDating_AppHost>(args);

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Start log collection for all application resources
        _logCollector = new LogCollector();
        var loggerService = _app.Services.GetRequiredService<ResourceLoggerService>();
        var logResources = _tenants.Select(t => $"{t}-bff")
            .Concat(["matching-worker", "conversation-worker"])
            .ToArray();
        _logCollector.StartWatching(loggerService, logResources);

        // Wait for all per-tenant BFFs to be healthy
        foreach (var tenant in _tenants)
        {
            await _app.ResourceNotifications.WaitForResourceHealthyAsync($"{tenant}-bff");
        }

        // Wait for all client apps to be responsive
        using var httpClient = new HttpClient();
        foreach (var tenant in _tenants)
        {
            var clientUrl = GetClientUrl(tenant);
            for (var i = 0; i < 30; i++)
            {
                try
                {
                    var response = await httpClient.GetAsync(clientUrl);
                    if (response.IsSuccessStatusCode) break;
                }
                catch
                {
                    // Client app not ready yet
                }
                await Task.Delay(1_000);
            }
        }

        // Initialize Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        if (_logCollector != null) await _logCollector.DisposeAsync();
        if (_app != null) await _app.DisposeAsync();
    }

    public static DistributedApplication App => _app ?? throw new InvalidOperationException("App not started");
    public static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not started");
    public static LogCollector LogCollector => _logCollector ?? throw new InvalidOperationException("Log collector not started");
    public static IReadOnlyList<string> Tenants => _tenants;

    /// <summary>Returns the BFF URL for the specified tenant (defaults to first tenant).</summary>
    public static string GetBffUrl(string? tenantId = null)
    {
        tenantId ??= _tenants[0];
        var name = $"{tenantId}-bff";
        return App.GetEndpoint(name, "http")?.ToString()
            ?? App.GetEndpoint(name, "https")?.ToString()
            ?? throw new InvalidOperationException($"BFF endpoint not found for tenant '{tenantId}'");
    }

    /// <summary>Returns the client app URL for the specified tenant (defaults to first tenant).</summary>
    public static string GetClientUrl(string? tenantId = null)
    {
        tenantId ??= _tenants[0];
        var name = $"{tenantId}-client";
        var url = App.GetEndpoint(name, "http")?.ToString()
            ?? throw new InvalidOperationException($"Client app endpoint not found for tenant '{tenantId}'");
        return url.TrimEnd('/');
    }
}
