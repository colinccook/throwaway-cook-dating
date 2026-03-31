using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class MatchingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage PageA => (IPage)_scenarioContext["Page"];

    public MatchingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("two users are actively looking")]
    public async Task GivenTwoUsersAreActivelyLooking()
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        // Create User A via API and inject auth into the main page
        var userAName = $"UserA-{Guid.NewGuid().ToString("N")[..8]}";
        var (tokenA, userIdA, emailA) = await CreateActiveUserViaApi(userAName, "Male", "Female");
        _scenarioContext["UserAName"] = userAName;

        await PageA.GotoAsync(clientUrl);
        await SetAuthInPage(PageA, tokenA, userIdA, emailA);

        // Create User B in a SEPARATE browser context (isolated localStorage)
        var userBName = $"UserB-{Guid.NewGuid().ToString("N")[..8]}";
        var (tokenB, userIdB, emailB) = await CreateActiveUserViaApi(userBName, "Female", "Male");
        _scenarioContext["UserBName"] = userBName;

        var contextB = await AspireHook.Browser.NewContextAsync();
        _scenarioContext["BrowserContextB"] = contextB;
        var pageB = await contextB.NewPageAsync();
        _scenarioContext["PageB"] = pageB;

        await pageB.GotoAsync(clientUrl);
        await SetAuthInPage(pageB, tokenB, userIdB, emailB);

        // Candidates are synced immediately via BFF, short delay for SignalR readiness
        await Task.Delay(1_000);
    }

    [Given("user A has swiped right on user B")]
    public async Task GivenUserAHasSwipedRightOnUserB()
    {
        var userBName = (string)_scenarioContext["UserBName"];
        await NavigateAndSwipeRightOn(PageA, userBName);
    }

    [When("user B swipes right on user A")]
    public async Task WhenUserBSwipesRightOnUserA()
    {
        var pageB = (IPage)_scenarioContext["PageB"];
        var userAName = (string)_scenarioContext["UserAName"];
        await NavigateAndSwipeRightOn(pageB, userAName);
    }

    [Then("a match should be created between them")]
    public async Task ThenAMatchShouldBeCreatedBetweenThem()
    {
        // User B triggered the mutual match and should see the match modal
        var pageB = (IPage)_scenarioContext["PageB"];
        await Assertions.Expect(pageB.GetByText("It's a Match!"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then("both users should be notified of the match")]
    public async Task ThenBothUsersShouldBeNotifiedOfTheMatch()
    {
        var pageB = (IPage)_scenarioContext["PageB"];

        // User B sees the match modal (triggered the mutual match)
        await Assertions.Expect(pageB.GetByText("It's a Match!"))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });

        // User A should also receive the MatchFound event via SignalR
        // (still connected to the hub on the discover page)
        await Assertions.Expect(PageA.GetByText("It's a Match!"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    /// <summary>
    /// Navigates to the discover tab and swipes right on the specified user,
    /// cycling through candidates via the UI swipe buttons.
    /// </summary>
    private async Task NavigateAndSwipeRightOn(IPage page, string targetUserName)
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        await page.GotoAsync($"{clientUrl}/discover");
        await Task.Delay(3_000);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var hasCard = await page.Locator(".swipe-card").IsVisibleAsync();
            var hasEmpty = await page.Locator(".discover-empty").IsVisibleAsync();
            var hasLoading = await page.Locator(".discover-status").IsVisibleAsync();

            if (hasCard)
            {
                var currentName = await page.Locator(".swipe-card-name").TextContentAsync();

                if (string.Equals(currentName?.Trim(), targetUserName, StringComparison.Ordinal))
                {
                    await page.Locator(".swipe-btn-like").ClickAsync();
                    await Task.Delay(1_000);
                    return;
                }

                await page.Locator(".swipe-btn-pass").ClickAsync();
                await Task.Delay(500);
                continue;
            }

            if (hasEmpty || hasLoading)
            {
                await Task.Delay(3_000);
                await page.GotoAsync($"{clientUrl}/discover");
                await Task.Delay(3_000);
                continue;
            }

            await Task.Delay(2_000);
        }

        throw new InvalidOperationException(
            $"Could not find candidate '{targetUserName}' after 30 attempts");
    }

    private static async Task SetAuthInPage(IPage page, string token, string userId, string email)
    {
        await page.EvaluateAsync(
            @"([t, id, e]) => {
                localStorage.setItem('auth_token', t);
                localStorage.setItem('auth_user', JSON.stringify({ id: id, email: e }));
            }",
            new object[] { token, userId, email });
    }

    private async Task<(string Token, string UserId, string Email)> CreateActiveUserViaApi(
        string displayName, string gender, string preferredGender)
    {
        var bffUrl = AspireHook.GetBffUrl().TrimEnd('/');
        var email = $"test-{Guid.NewGuid():N}@example.com";

        using var httpClient = new HttpClient();

        var signupResponse = await httpClient.PostAsJsonAsync($"{bffUrl}/api/auth/signup", new
        {
            email,
            password = "TestPass123!",
            displayName,
            dateOfBirth = "1995-06-15",
            gender,
            preferredGender,
            minAge = 18,
            maxAge = 50,
            maxDistanceKm = 100
        });
        signupResponse.EnsureSuccessStatusCode();

        var result = await signupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = result.TryGetProperty("accessToken", out var at)
            ? at.GetString()!
            : result.GetProperty("token").GetString()!;
        var userId = result.GetProperty("userId").GetString()!;

        // Set the user to actively looking
        using var statusRequest = new HttpRequestMessage(HttpMethod.Put, $"{bffUrl}/api/profile/status")
        {
            Content = JsonContent.Create(new { status = "ActivelyLooking" })
        };
        statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var statusResponse = await httpClient.SendAsync(statusRequest);
        statusResponse.EnsureSuccessStatusCode();

        return (token, userId, email);
    }
}
