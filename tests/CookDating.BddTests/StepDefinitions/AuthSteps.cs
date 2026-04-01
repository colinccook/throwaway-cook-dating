using System.Text.RegularExpressions;
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
        await Page.GotoAsync($"{clientUrl}/signup");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sign Up");
    }

    [When("I enter valid sign up details")]
    public async Task WhenIEnterValidSignUpDetails()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        _scenarioContext["TestEmail"] = email;
        _scenarioContext["TestPassword"] = "TestPass123!";
        _scenarioContext["TestDisplayName"] = "Test User";
        _scenarioContext["TestDateOfBirth"] = "1995-06-15";

        await FillSignUpFormAsync(
            email,
            (string)_scenarioContext["TestPassword"],
            (string)_scenarioContext["TestDisplayName"],
            (string)_scenarioContext["TestDateOfBirth"]);
    }

    [When("I enter sign up details with an underage date of birth")]
    public async Task WhenIEnterSignUpDetailsWithAnUnderageDateOfBirth()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        _scenarioContext["TestEmail"] = email;
        _scenarioContext["TestPassword"] = "TestPass123!";
        _scenarioContext["TestDisplayName"] = "Test User";
        _scenarioContext["InvalidDateOfBirth"] = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)).ToString("yyyy-MM-dd");
        _scenarioContext["CorrectedDateOfBirth"] = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)).ToString("yyyy-MM-dd");

        await FillSignUpFormAsync(
            email,
            (string)_scenarioContext["TestPassword"],
            (string)_scenarioContext["TestDisplayName"],
            (string)_scenarioContext["InvalidDateOfBirth"]);
    }

    [When("I submit the sign up form")]
    public async Task WhenISubmitTheSignUpForm()
    {
        await Page.Locator("button[type='submit']").ClickAsync();
    }

    [Then("I should be redirected to the profile page")]
    public async Task ThenIShouldBeRedirectedToTheProfilePage()
    {
        await Expect(Page).ToHaveURLAsync(new Regex(".*/profile$"), new() { Timeout = 30000 });
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

    [Then("I should see a date of birth validation error")]
    public async Task ThenIShouldSeeADateOfBirthValidationError()
    {
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sign Up");
        await Expect(Page.GetByText("Must be at least 18 years old"))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Then("no account should be created for the invalid submission")]
    public async Task ThenNoAccountShouldBeCreatedForTheInvalidSubmission()
    {
        await Page.WaitForURLAsync("**/signup");

        var authToken = await Page.EvaluateAsync<string?>("() => localStorage.getItem('auth_token')");
        var authUser = await Page.EvaluateAsync<string?>("() => localStorage.getItem('auth_user')");

        Assert.That(authToken, Is.Null, "No auth token should be stored after invalid sign-up submission");
        Assert.That(authUser, Is.Null, "No authenticated user should be stored after invalid sign-up submission");
    }

    [When("I correct only the date of birth")]
    public async Task WhenICorrectOnlyTheDateOfBirth()
    {
        var email = (string)_scenarioContext["TestEmail"];
        var displayName = (string)_scenarioContext["TestDisplayName"];
        var correctedDateOfBirth = (string)_scenarioContext["CorrectedDateOfBirth"];

        await Expect(Page.Locator("#email")).ToHaveValueAsync(email);
        await Expect(Page.Locator("#displayName")).ToHaveValueAsync(displayName);
        await Page.Locator("#password").FillAsync((string)_scenarioContext["TestPassword"]);
        await Page.Locator("#dateOfBirth").FillAsync(correctedDateOfBirth);
    }

    [Then(@"I should not see a sign up error containing ""(.*)""")]
    public async Task ThenIShouldNotSeeASignUpErrorContaining(string unexpectedMessage)
    {
        await Expect(Page.GetByText(unexpectedMessage)).ToHaveCountAsync(0);
    }

    [Then("the retry sign up should not hit an already registered error")]
    public async Task ThenTheRetrySignUpShouldNotHitAnAlreadyRegisteredError()
    {
        await Expect(Page.GetByText("Email already registered")).ToHaveCountAsync(0);
    }

    [Given("I am logged in")]
    public async Task GivenIAmLoggedIn()
    {
        var clientUrl = AspireHook.GetClientUrl();
        var email = $"test-{Guid.NewGuid():N}@example.com";
        _scenarioContext["TestEmail"] = email;
        _scenarioContext["TestPassword"] = "TestPass123!";
        _scenarioContext["TestDisplayName"] = "Test User";
        _scenarioContext["TestDateOfBirth"] = "1995-06-15";

        await Page.GotoAsync($"{clientUrl}/signup");
        await Expect(Page.Locator("h1")).ToHaveTextAsync("Sign Up");

        await FillSignUpFormAsync(
            email,
            (string)_scenarioContext["TestPassword"],
            (string)_scenarioContext["TestDisplayName"],
            (string)_scenarioContext["TestDateOfBirth"]);
        await Page.Locator("button[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/profile");
    }

    [Given("I am logged in and actively looking")]
    public async Task GivenIAmLoggedInAndActivelyLooking()
    {
        await GivenIAmLoggedIn();

        // Wait for profile to load, then toggle status to Actively Looking
        await Expect(Page.Locator("button.looking-toggle")).ToBeVisibleAsync();
        var toggle = Page.Locator("button.looking-toggle");

        // If currently "Not Looking", click to toggle to "Actively Looking"
        var text = await toggle.TextContentAsync();
        if (text != null && text.Contains("Not Looking"))
        {
            await toggle.ClickAsync();
            await Expect(toggle).ToContainTextAsync("Actively Looking");
        }
    }

    private async Task FillSignUpFormAsync(string email, string password, string displayName, string dateOfBirth)
    {
        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#displayName").FillAsync(displayName);
        await Page.Locator("#dateOfBirth").FillAsync(dateOfBirth);
        await Page.Locator("#gender").SelectOptionAsync("Male");
        await Page.Locator("#minAge").FillAsync("18");
        await Page.Locator("#maxAge").FillAsync("35");
        await Page.Locator("#maxDistanceKm").FillAsync("50");
    }
}
