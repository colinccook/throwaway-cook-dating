using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class ConversationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage PageA => (IPage)_scenarioContext["Page"];

    public ConversationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I am matched with another user")]
    public async Task GivenIAmMatchedWithAnotherUser()
    {
        var context = (IBrowserContext)_scenarioContext["BrowserContext"];
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        // Create User A via API
        var userAName = $"ConvA-{Guid.NewGuid().ToString("N")[..8]}";
        var (tokenA, userIdA, emailA) = await CreateActiveUserViaApi(userAName, "Male", "Female");
        _scenarioContext["UserAName"] = userAName;
        _scenarioContext["TokenA"] = tokenA;

        await PageA.GotoAsync(clientUrl);
        await SetAuthInPage(PageA, tokenA, userIdA, emailA);

        // Create User B via API and inject auth into a second page
        var userBName = $"ConvB-{Guid.NewGuid().ToString("N")[..8]}";
        var (tokenB, userIdB, emailB) = await CreateActiveUserViaApi(userBName, "Female", "Male");
        _scenarioContext["UserBName"] = userBName;
        _scenarioContext["TokenB"] = tokenB;

        var pageB = await context.NewPageAsync();
        _scenarioContext["PageB"] = pageB;

        await pageB.GotoAsync(clientUrl);
        await SetAuthInPage(pageB, tokenB, userIdB, emailB);

        // User A swipes right on User B
        await NavigateAndSwipeRightOn(PageA, userBName);

        // User B swipes right on User A (triggers mutual match)
        await NavigateAndSwipeRightOn(pageB, userAName);

        // Verify match modal appears on User B's page
        await Assertions.Expect(pageB.GetByText("It's a Match!"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Wait for the conversation worker to create the conversation
        await Task.Delay(3_000);
    }

    [Given("I open the conversation with my match")]
    public async Task GivenIOpenTheConversationWithMyMatch()
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        await PageA.GotoAsync($"{clientUrl}/matches");

        // Wait for a match list item to appear (conversation worker may need a moment)
        await PageA.Locator(".match-list-item").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        // Click the first match to open the chat view
        await PageA.Locator(".match-list-item").First.ClickAsync();

        // Wait for the chat view to load
        await PageA.Locator(".chat-view").Or(PageA.Locator(".messages-container"))
            .First.WaitForAsync(new() { Timeout = 10_000 });
    }

    [When("I type and send a message")]
    public async Task WhenITypeAndSendAMessage()
    {
        var messageText = $"Hello from test {Guid.NewGuid().ToString("N")[..8]}";
        _scenarioContext["TestMessage"] = messageText;

        await PageA.Locator(".chat-input input").FillAsync(messageText);
        await PageA.Locator(".chat-input button").ClickAsync();
    }

    [Then("the message should appear in the conversation")]
    public async Task ThenTheMessageShouldAppearInTheConversation()
    {
        var messageText = (string)_scenarioContext["TestMessage"];

        // Wait for the sent message to appear in the messages container
        await Assertions.Expect(PageA.Locator(".messages-container").GetByText(messageText))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then("the other user should receive the message")]
    public async Task ThenTheOtherUserShouldReceiveTheMessage()
    {
        var pageB = (IPage)_scenarioContext["PageB"];
        var messageText = (string)_scenarioContext["TestMessage"];
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        // Navigate User B to the matches tab
        await pageB.GotoAsync($"{clientUrl}/matches");

        // Wait for the match list item to appear
        await pageB.Locator(".match-list-item").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        // Click the conversation to open it
        await pageB.Locator(".match-list-item").First.ClickAsync();

        // Wait for the chat view to load
        await pageB.Locator(".chat-view").Or(pageB.Locator(".messages-container"))
            .First.WaitForAsync(new() { Timeout = 10_000 });

        // Verify the message from User A is visible on User B's page
        await Assertions.Expect(pageB.Locator(".messages-container").GetByText(messageText))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Given("I am not matched with another user")]
    public async Task GivenIAmNotMatchedWithAnotherUser()
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        // Create a single user who is not matched with anyone
        var userName = $"Solo-{Guid.NewGuid().ToString("N")[..8]}";
        var (token, userId, email) = await CreateActiveUserViaApi(userName, "Male", "Female");

        await PageA.GotoAsync(clientUrl);
        await SetAuthInPage(PageA, token, userId, email);
    }

    [Then("I should not be able to send them a message")]
    public async Task ThenIShouldNotBeAbleToSendThemAMessage()
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        // Navigate to the matches tab
        await PageA.GotoAsync($"{clientUrl}/matches");

        // Wait for the page to finish loading — expect the empty state
        await PageA.Locator(".matches-empty, .match-list-item")
            .First.WaitForAsync(new() { Timeout = 15_000 });

        // Verify the empty state is shown (no matches → no conversations)
        await Assertions.Expect(PageA.Locator(".matches-empty"))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });

        // Verify there are no match list items to click into
        await Assertions.Expect(PageA.Locator(".match-list-item"))
            .ToHaveCountAsync(0);
    }

    private async Task NavigateAndSwipeRightOn(IPage page, string targetUserName)
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await page.GotoAsync($"{clientUrl}/discover");
            await page.Locator(".swipe-card, .discover-empty").First
                .WaitForAsync(new() { Timeout = 30_000 });

            if (await page.Locator(".discover-empty").IsVisibleAsync())
                throw new InvalidOperationException(
                    $"No candidates available while looking for '{targetUserName}'");

            var currentName = await page.Locator(".swipe-card-name").TextContentAsync();

            if (string.Equals(currentName?.Trim(), targetUserName, StringComparison.Ordinal))
            {
                await page.Locator(".swipe-btn-like").ClickAsync();
                await Task.Delay(1_000);
                return;
            }

            // Not the target – swipe left to skip
            await page.Locator(".swipe-btn-pass").ClickAsync();
            await Task.Delay(500);
        }

        throw new InvalidOperationException(
            $"Could not find candidate '{targetUserName}' after 20 attempts");
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

    private static async Task<(string Token, string UserId, string Email)> CreateActiveUserViaApi(
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
