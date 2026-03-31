using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class MatchingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public MatchingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("two users are actively looking")]
    public async Task GivenTwoUsersAreActivelyLooking()
    {
        throw new PendingStepException();
    }

    [Given("user A has swiped right on user B")]
    public async Task GivenUserAHasSwipedRightOnUserB()
    {
        throw new PendingStepException();
    }

    [When("user B swipes right on user A")]
    public async Task WhenUserBSwipesRightOnUserA()
    {
        throw new PendingStepException();
    }

    [Then("a match should be created between them")]
    public async Task ThenAMatchShouldBeCreatedBetweenThem()
    {
        throw new PendingStepException();
    }

    [Then("both users should be notified of the match")]
    public async Task ThenBothUsersShouldBeNotifiedOfTheMatch()
    {
        throw new PendingStepException();
    }
}
