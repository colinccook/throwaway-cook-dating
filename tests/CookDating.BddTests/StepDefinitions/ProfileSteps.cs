using Microsoft.Playwright;
using Reqnroll;

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
        throw new PendingStepException();
    }

    [When(@"I toggle my status to ""(.*)""")]
    public async Task WhenIToggleMyStatusTo(string status)
    {
        throw new PendingStepException();
    }

    [Then(@"my status should show ""(.*)""")]
    public async Task ThenMyStatusShouldShow(string status)
    {
        throw new PendingStepException();
    }

    [Then("a looking status changed event should be raised")]
    public async Task ThenALookingStatusChangedEventShouldBeRaised()
    {
        throw new PendingStepException();
    }
}
