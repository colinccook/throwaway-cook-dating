using Microsoft.Playwright;
using Reqnroll;

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
        throw new PendingStepException();
    }

    [When("I enter valid sign up details")]
    public async Task WhenIEnterValidSignUpDetails()
    {
        throw new PendingStepException();
    }

    [When("I submit the sign up form")]
    public async Task WhenISubmitTheSignUpForm()
    {
        throw new PendingStepException();
    }

    [Then("I should be redirected to the profile page")]
    public async Task ThenIShouldBeRedirectedToTheProfilePage()
    {
        throw new PendingStepException();
    }

    [Then("my profile should be created")]
    public async Task ThenMyProfileShouldBeCreated()
    {
        throw new PendingStepException();
    }

    [Given("I am logged in")]
    public async Task GivenIAmLoggedIn()
    {
        throw new PendingStepException();
    }

    [Given("I am logged in and actively looking")]
    public async Task GivenIAmLoggedInAndActivelyLooking()
    {
        throw new PendingStepException();
    }
}
