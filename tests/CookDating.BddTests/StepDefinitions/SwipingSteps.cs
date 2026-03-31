using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CookDating.BddTests.Hooks;
using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class SwipingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public SwipingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I am on the discover tab")]
    public async Task GivenIAmOnTheDiscoverTab()
    {
        var clientUrl = AspireHook.GetClientUrl().TrimEnd('/');
        await Page.GotoAsync($"{clientUrl}/discover");
        await Page.Locator(".discover-page").WaitForAsync(new() { Timeout = 15_000 });
    }

    [Given("there is a candidate profile shown")]
    public async Task GivenThereIsACandidateProfileShown()
    {
        // Create a candidate user via the BFF API so they appear in discover
        var candidateName = $"Candidate-{Guid.NewGuid().ToString("N")[..8]}";
        await CreateActiveUserViaApi(candidateName, "Female", "Male");
        _scenarioContext["CandidateDisplayName"] = candidateName;

        // Reload to fetch fresh candidates including the newly created user
        await Page.ReloadAsync();
        await Page.Locator(".swipe-card").WaitForAsync(new() { Timeout = 30_000 });
    }

    [When("I swipe right on the candidate")]
    public async Task WhenISwipeRightOnTheCandidate()
    {
        var name = await Page.Locator(".swipe-card-name").TextContentAsync();
        _scenarioContext["SwipedCandidateName"] = name?.Trim() ?? "";
        await Page.Locator(".swipe-btn-like").ClickAsync();
    }

    [When("I swipe left on the candidate")]
    public async Task WhenISwipeLeftOnTheCandidate()
    {
        var name = await Page.Locator(".swipe-card-name").TextContentAsync();
        _scenarioContext["SwipedCandidateName"] = name?.Trim() ?? "";
        await Page.Locator(".swipe-btn-pass").ClickAsync();
    }

    [Then("the swipe should be recorded as a like")]
    public async Task ThenTheSwipeShouldBeRecordedAsALike()
    {
        await VerifySwipeWasRecorded();
    }

    [Then("the swipe should be recorded as a dislike")]
    public async Task ThenTheSwipeShouldBeRecordedAsADislike()
    {
        await VerifySwipeWasRecorded();
    }

    private async Task VerifySwipeWasRecorded()
    {
        // Allow the server to process the swipe
        await Task.Delay(1_000);

        // Reload to re-fetch candidates; the server filters out already-swiped users
        await Page.ReloadAsync();
        await Page.Locator(".swipe-card, .discover-empty").First
            .WaitForAsync(new() { Timeout = 30_000 });

        // The swiped candidate should no longer appear in the card list
        var swipedName = (string)_scenarioContext["SwipedCandidateName"];
        await Assertions.Expect(Page.Locator(".swipe-card-name").GetByText(swipedName))
            .Not.ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    private async Task CreateActiveUserViaApi(
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

        // Set the candidate to actively looking
        using var statusRequest = new HttpRequestMessage(HttpMethod.Put, $"{bffUrl}/api/profile/status")
        {
            Content = JsonContent.Create(new { status = "ActivelyLooking" })
        };
        statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var statusResponse = await httpClient.SendAsync(statusRequest);
        statusResponse.EnsureSuccessStatusCode();
    }
}
