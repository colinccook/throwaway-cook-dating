using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.Hooks;

[Binding]
public sealed class AspireHook
{
    private static DistributedApplication? _app;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        // Start the Aspire app
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CookDating_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for the BFF to be ready
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("bff");

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
        if (_app != null) await _app.DisposeAsync();
    }

    public static DistributedApplication App => _app ?? throw new InvalidOperationException("App not started");
    public static IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not started");

    public static string GetBffUrl()
    {
        return App.GetEndpoint("bff", "https")?.ToString()
            ?? App.GetEndpoint("bff", "http")?.ToString()
            ?? throw new InvalidOperationException("BFF endpoint not found");
    }

    public static string GetClientUrl()
    {
        return App.GetEndpoint("client-app", "http")?.ToString()
            ?? throw new InvalidOperationException("Client app endpoint not found");
    }
}
