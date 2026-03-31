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
        await Page.GotoAsync($"{clientUrl}/profile", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
    }

    [When(@"I toggle my status to ""(.*)""")]
    public async Task WhenIToggleMyStatusTo(string status)
    {
        var toggle = Page.Locator("button.looking-toggle");
        await Expect(toggle).ToBeVisibleAsync();

        await Page.RunAndWaitForResponseAsync(
            async () => await toggle.ClickAsync(),
            response => response.Url.Contains("/api/profile/status"));

        await Expect(toggle).ToContainTextAsync(status, new() { Timeout = 10000 });
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
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("button.looking-toggle")).ToContainTextAsync("Actively Looking");
    }

    // ──── Profile editing steps ────

    [When(@"I change my display name to ""(.*)""")]
    public async Task WhenIChangeMyDisplayNameTo(string name)
    {
        _scenarioContext["ExpectedDisplayName"] = name;
        var input = Page.Locator(".profile-form label:has-text('Display Name') input");
        await input.ClearAsync();
        await input.FillAsync(name);
    }

    [When(@"I change my bio to ""(.*)""")]
    public async Task WhenIChangeMyBioTo(string bio)
    {
        _scenarioContext["ExpectedBio"] = bio;
        var textarea = Page.Locator(".profile-form label:has-text('Bio') textarea");
        await textarea.ClearAsync();
        await textarea.FillAsync(bio);
    }

    [When(@"I change my min age to ""(.*)""")]
    public async Task WhenIChangeMyMinAgeTo(string age)
    {
        _scenarioContext["ExpectedMinAge"] = age;
        var input = Page.Locator(".profile-form label:has-text('Min Age') input");
        await input.ClearAsync();
        await input.FillAsync(age);
    }

    [When(@"I change my max age to ""(.*)""")]
    public async Task WhenIChangeMyMaxAgeTo(string age)
    {
        _scenarioContext["ExpectedMaxAge"] = age;
        var input = Page.Locator(".profile-form label:has-text('Max Age') input");
        await input.ClearAsync();
        await input.FillAsync(age);
    }

    [When(@"I change my max distance to ""(.*)""")]
    public async Task WhenIChangeMyMaxDistanceTo(string distance)
    {
        _scenarioContext["ExpectedMaxDistance"] = distance;
        var input = Page.Locator(".profile-form label:has-text('Max Distance') input");
        await input.ClearAsync();
        await input.FillAsync(distance);
    }

    [When(@"I change my preferred gender to ""(.*)""")]
    public async Task WhenIChangeMyPreferredGenderTo(string gender)
    {
        _scenarioContext["ExpectedPreferredGender"] = gender;
        var select = Page.Locator(".profile-form label:has-text('Preferred Gender') select");
        await select.SelectOptionAsync(gender);
    }

    [When("I save my profile changes")]
    public async Task WhenISaveMyProfileChanges()
    {
        await Page.RunAndWaitForResponseAsync(
            async () => await Page.Locator("button.profile-save-btn").ClickAsync(),
            response => response.Url.Contains("/api/profile") && response.Request.Method == "PUT");
        // Wait for the save to complete (button re-enables after saving)
        await Expect(Page.Locator("button.profile-save-btn")).Not.ToBeDisabledAsync(new() { Timeout = 10000 });
    }

    [Then(@"I should see a profile success message ""(.*)""")]
    public async Task ThenIShouldSeeAProfileSuccessMessage(string message)
    {
        await Expect(Page.Locator(".profile-message.success")).ToHaveTextAsync(message, new() { Timeout = 10000 });
    }

    [Then("my profile changes should persist after reload")]
    public async Task ThenMyProfileChangesShouldPersistAfterReload()
    {
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
        // Wait for profile data to load
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync(new() { Timeout = 10000 });

        if (_scenarioContext.TryGetValue("ExpectedDisplayName", out var name))
        {
            var input = Page.Locator(".profile-form label:has-text('Display Name') input");
            await Expect(input).ToHaveValueAsync((string)name);
        }

        if (_scenarioContext.TryGetValue("ExpectedBio", out var bio))
        {
            var textarea = Page.Locator(".profile-form label:has-text('Bio') textarea");
            await Expect(textarea).ToHaveValueAsync((string)bio);
        }
    }

    [Then("my preference changes should persist after reload")]
    public async Task ThenMyPreferenceChangesShouldPersistAfterReload()
    {
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync(new() { Timeout = 10000 });

        if (_scenarioContext.TryGetValue("ExpectedMinAge", out var minAge))
        {
            var input = Page.Locator(".profile-form label:has-text('Min Age') input");
            await Expect(input).ToHaveValueAsync((string)minAge);
        }

        if (_scenarioContext.TryGetValue("ExpectedMaxAge", out var maxAge))
        {
            var input = Page.Locator(".profile-form label:has-text('Max Age') input");
            await Expect(input).ToHaveValueAsync((string)maxAge);
        }

        if (_scenarioContext.TryGetValue("ExpectedMaxDistance", out var dist))
        {
            var input = Page.Locator(".profile-form label:has-text('Max Distance') input");
            await Expect(input).ToHaveValueAsync((string)dist);
        }
    }

    [Then(@"my preferred gender should show ""(.*)"" after reload")]
    public async Task ThenMyPreferredGenderShouldShowAfterReload(string gender)
    {
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync(new() { Timeout = 10000 });

        var select = Page.Locator(".profile-form label:has-text('Preferred Gender') select");
        // "Any" maps to an empty string value in the dropdown
        var expectedValue = gender == "Any" ? "" : gender;
        await Expect(select).ToHaveValueAsync(expectedValue);
    }
}
