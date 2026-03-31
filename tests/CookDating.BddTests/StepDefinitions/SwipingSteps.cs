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
        throw new PendingStepException();
    }

    [Given("there is a candidate profile shown")]
    public async Task GivenThereIsACandidateProfileShown()
    {
        throw new PendingStepException();
    }

    [When("I swipe right on the candidate")]
    public async Task WhenISwipeRightOnTheCandidate()
    {
        throw new PendingStepException();
    }

    [When("I swipe left on the candidate")]
    public async Task WhenISwipeLeftOnTheCandidate()
    {
        throw new PendingStepException();
    }

    [Then("the swipe should be recorded as a like")]
    public async Task ThenTheSwipeShouldBeRecordedAsALike()
    {
        throw new PendingStepException();
    }

    [Then("the swipe should be recorded as a dislike")]
    public async Task ThenTheSwipeShouldBeRecordedAsADislike()
    {
        throw new PendingStepException();
    }
}
