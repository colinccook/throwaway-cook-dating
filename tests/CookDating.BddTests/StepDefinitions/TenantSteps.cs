using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class TenantSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public TenantSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("a user is registered on the Cook Dating tenant")]
    public async Task GivenAUserIsRegisteredOnTheCookDatingTenant()
    {
        var (email, password) = await CreateUserOnTenant("cook-dating");
        _scenarioContext["CrossTenantEmail"] = email;
        _scenarioContext["CrossTenantPassword"] = password;
    }

    [When("the user tries to sign in on the Tech Dating tenant")]
    public async Task WhenTheUserTriesToSignInOnTheTechDatingTenant()
    {
        var clientUrl = AspireHook.GetClientUrl("tech-dating");
        var email = (string)_scenarioContext["CrossTenantEmail"];
        var password = (string)_scenarioContext["CrossTenantPassword"];

        await Page.GotoAsync($"{clientUrl}/signin");
        await Assertions.Expect(Page.Locator("h1")).ToHaveTextAsync("Sign In");

        await Page.Locator("#email").FillAsync(email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("button[type='submit']").ClickAsync();
    }

    [Then("the sign in should fail with an authentication error")]
    public async Task ThenTheSignInShouldFailWithAnAuthenticationError()
    {
        // Should remain on sign-in page with an error message
        await Assertions.Expect(Page.Locator("p[style*='color']"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.WaitForURLAsync("**/signin");
    }

    [Given("an actively looking user on the Cook Dating tenant")]
    public async Task GivenAnActivelyLookingUserOnTheCookDatingTenant()
    {
        var (_, _, displayName, token) = await CreateActiveUserOnTenant("cook-dating");
        _scenarioContext["CookDatingUser"] = displayName;
        _scenarioContext["CookDatingToken"] = token;
    }

    [Given("an actively looking user on the Tech Dating tenant")]
    public async Task GivenAnActivelyLookingUserOnTheTechDatingTenant()
    {
        var (_, _, displayName, _) = await CreateActiveUserOnTenant("tech-dating");
        _scenarioContext["TechDatingUser"] = displayName;
    }

    [When("the Cook Dating user views the discover page")]
    public async Task WhenTheCookDatingUserViewsTheDiscoverPage()
    {
        var clientUrl = AspireHook.GetClientUrl("cook-dating");
        var token = (string)_scenarioContext["CookDatingToken"];

        await Page.GotoAsync(clientUrl);
        await Page.EvaluateAsync(
            @"([t]) => {
                localStorage.setItem('auth_token', t);
                localStorage.setItem('auth_user', JSON.stringify({ id: 'x', email: 'x@x.com' }));
            }",
            new object[] { token });

        await Page.GotoAsync($"{clientUrl}/discover");
        // Wait for candidates to load or empty state to appear
        await Task.Delay(3_000);
    }

    [Then("they should not see the Tech Dating user as a candidate")]
    public async Task ThenTheyShouldNotSeeTheTechDatingUserAsACandidate()
    {
        var techUser = (string)_scenarioContext["TechDatingUser"];

        // Check all visible candidate names — the tech dating user should never appear
        var cards = Page.Locator(".swipe-card-name");
        var count = await cards.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var name = await cards.Nth(i).TextContentAsync();
            Assert.That(name?.Trim(), Is.Not.EqualTo(techUser),
                $"Tech Dating user '{techUser}' should not appear on Cook Dating discover page");
        }
    }

    private static async Task<(string Email, string Password)> CreateUserOnTenant(string tenantId)
    {
        var bffUrl = AspireHook.GetBffUrl(tenantId).TrimEnd('/');
        var email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "TestPass123!";

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsJsonAsync($"{bffUrl}/api/auth/signup", new
        {
            email,
            password,
            displayName = $"User-{Guid.NewGuid().ToString("N")[..6]}",
            dateOfBirth = "1995-06-15",
            gender = "Male",
            preferredGender = "Female",
            minAge = 18,
            maxAge = 50,
            maxDistanceKm = 100
        });
        response.EnsureSuccessStatusCode();

        return (email, password);
    }

    private static async Task<(string Email, string Password, string DisplayName, string Token)>
        CreateActiveUserOnTenant(string tenantId)
    {
        var bffUrl = AspireHook.GetBffUrl(tenantId).TrimEnd('/');
        var email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "TestPass123!";
        var displayName = $"User-{Guid.NewGuid().ToString("N")[..6]}";

        using var httpClient = new HttpClient();
        var signupResponse = await httpClient.PostAsJsonAsync($"{bffUrl}/api/auth/signup", new
        {
            email,
            password,
            displayName,
            dateOfBirth = "1995-06-15",
            gender = "Male",
            preferredGender = "Female",
            minAge = 18,
            maxAge = 50,
            maxDistanceKm = 100
        });
        signupResponse.EnsureSuccessStatusCode();

        var result = await signupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = result.TryGetProperty("accessToken", out var at)
            ? at.GetString()!
            : result.GetProperty("token").GetString()!;

        // Set the user to actively looking
        using var statusRequest = new HttpRequestMessage(HttpMethod.Put, $"{bffUrl}/api/profile/status")
        {
            Content = JsonContent.Create(new { status = "ActivelyLooking" })
        };
        statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var statusResponse = await httpClient.SendAsync(statusRequest);
        statusResponse.EnsureSuccessStatusCode();

        return (email, password, displayName, token);
    }
}
