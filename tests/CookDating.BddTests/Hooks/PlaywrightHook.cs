using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.Hooks;

[Binding]
public sealed class PlaywrightHook
{
    private readonly ScenarioContext _scenarioContext;

    public PlaywrightHook(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var context = await AspireHook.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        _scenarioContext["BrowserContext"] = context;
        _scenarioContext["Page"] = page;
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        if (_scenarioContext.TryGetValue("BrowserContext", out var ctx) && ctx is IBrowserContext context)
        {
            await context.CloseAsync();
        }
    }
}
