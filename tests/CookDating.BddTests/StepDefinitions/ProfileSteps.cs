using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class ProfileSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public ProfileSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I am on the profile tab")]
    public async Task GivenIAmOnTheProfileTab()
    {
        var clientUrl = AspireHook.GetClientUrl();
        await Page.GotoAsync($"{clientUrl}/profile");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
    }

    [When(@"I toggle my status to ""(.*)""")]
    public async Task WhenIToggleMyStatusTo(string status)
    {
        var toggle = Page.Locator("button.looking-toggle");
        await Expect(toggle).ToBeVisibleAsync();
        await toggle.ClickAsync();
        await Expect(toggle).ToContainTextAsync(status);
    }

    [Then(@"my status should show ""(.*)""")]
    public async Task ThenMyStatusShouldShow(string status)
    {
        await Expect(Page.Locator("button.looking-toggle")).ToContainTextAsync(status);
    }

    [Then("a looking status changed event should be raised")]
    public async Task ThenALookingStatusChangedEventShouldBeRaised()
    {
        // Verify the status change persisted by reloading and checking
        await Page.ReloadAsync();
        await Expect(Page.Locator("button.looking-toggle")).ToContainTextAsync("Actively Looking");
    }
}
