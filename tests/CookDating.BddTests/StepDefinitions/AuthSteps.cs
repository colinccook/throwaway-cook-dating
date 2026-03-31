using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class AuthSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public AuthSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I am on the sign up page")]
    public async Task GivenIAmOnTheSignUpPage()
    {
        var clientUrl = AspireHook.GetClientUrl();
        await Page.GotoAsync($"{clientUrl}/signup", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sign Up");
    }

    [When("I enter valid sign up details")]
    public async Task WhenIEnterValidSignUpDetails()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        _scenarioContext["TestEmail"] = email;
        _scenarioContext["TestPassword"] = "TestPass123!";
        _scenarioContext["TestDisplayName"] = "Test User";

        await FillSignUpFormAsync(email, "TestPass123!", "Test User");
    }

    [When("I submit the sign up form")]
    public async Task WhenISubmitTheSignUpForm()
    {
        await Page.Locator("button[type='submit']").ClickAsync();
    }

    [Then("I should be redirected to the profile page")]
    public async Task ThenIShouldBeRedirectedToTheProfilePage()
    {
        await Page.WaitForURLAsync("**/profile", new() { Timeout = 15_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Then("my profile should be created")]
    public async Task ThenMyProfileShouldBeCreated()
    {
        var displayName = (string)_scenarioContext["TestDisplayName"];
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Profile");
        // Wait for profile data to load (looking toggle becomes visible)
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync(new() { Timeout = 10000 });
        // Display name is in an input field, so check the value attribute
        await Expect(Page.Locator(".profile-page input[type='text']").First).ToHaveValueAsync(displayName, new() { Timeout = 10000 });
    }

    [Given("I am logged in")]
    public async Task GivenIAmLoggedIn()
    {
        var clientUrl = AspireHook.GetClientUrl();
        var email = $"test-{Guid.NewGuid():N}@example.com";
        _scenarioContext["TestEmail"] = email;
        _scenarioContext["TestPassword"] = "TestPass123!";
        _scenarioContext["TestDisplayName"] = "Test User";

        await Page.GotoAsync($"{clientUrl}/signup", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sign Up");

        await FillSignUpFormAsync(email, "TestPass123!", "Test User");
        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/profile", new() { Timeout = 15_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Given("I am logged in and actively looking")]
    public async Task GivenIAmLoggedInAndActivelyLooking()
    {
        await GivenIAmLoggedIn();

        // Wait for profile to load, then toggle status to Actively Looking
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        var toggle = Page.Locator("button.looking-toggle");

        // If currently "Not Looking", click to toggle to "Actively Looking"
        var text = await toggle.TextContentAsync();
        if (text != null && text.Contains("Not Looking"))
        {
            await toggle.ClickAsync();
            await Expect(toggle).ToContainTextAsync("Actively Looking", new() { Timeout = 10_000 });
        }
    }

    private async Task FillSignUpFormAsync(string email, string password, string displayName)
    {
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#displayName").FillAsync(displayName);
        await Page.Locator("#dateOfBirth").FillAsync("1995-06-15");
        await Page.Locator("#gender").SelectOptionAsync("Male");
        await Page.Locator("#minAge").FillAsync("18");
        await Page.Locator("#maxAge").FillAsync("35");
        await Page.Locator("#maxDistanceKm").FillAsync("50");
    }
}
