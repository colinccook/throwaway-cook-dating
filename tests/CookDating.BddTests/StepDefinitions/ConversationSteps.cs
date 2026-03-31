using Microsoft.Playwright;
using Reqnroll;

namespace CookDating.BddTests.StepDefinitions;

[Binding]
public class ConversationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private IPage Page => (IPage)_scenarioContext["Page"];

    public ConversationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I am matched with another user")]
    public async Task GivenIAmMatchedWithAnotherUser()
    {
        throw new PendingStepException();
    }

    [Given("I open the conversation with my match")]
    public async Task GivenIOpenTheConversationWithMyMatch()
    {
        throw new PendingStepException();
    }

    [When("I type and send a message")]
    public async Task WhenITypeAndSendAMessage()
    {
        throw new PendingStepException();
    }

    [Then("the message should appear in the conversation")]
    public async Task ThenTheMessageShouldAppearInTheConversation()
    {
        throw new PendingStepException();
    }

    [Then("the other user should receive the message")]
    public async Task ThenTheOtherUserShouldReceiveTheMessage()
    {
        throw new PendingStepException();
    }

    [Given("I am not matched with another user")]
    public async Task GivenIAmNotMatchedWithAnotherUser()
    {
        throw new PendingStepException();
    }

    [Then("I should not be able to send them a message")]
    public async Task ThenIShouldNotBeAbleToSendThemAMessage()
    {
        throw new PendingStepException();
    }
}
