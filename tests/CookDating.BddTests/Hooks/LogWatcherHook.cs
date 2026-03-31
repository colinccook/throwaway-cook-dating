using CookDating.BddTests.Support;
using Reqnroll;

namespace CookDating.BddTests.Hooks;

/// <summary>
/// Reqnroll hook that checks for unexpected error/warning logs after
/// each scenario. Fails the scenario if any service emitted errors
/// that don't match known expected patterns.
/// </summary>
[Binding]
public sealed class LogWatcherHook
{
    private readonly ScenarioContext _scenarioContext;

    public LogWatcherHook(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = 0)]
    public void MarkLogStart()
    {
        AspireHook.LogCollector.MarkScenarioStart();
    }

    [AfterScenario(Order = 100)]
    public void CheckForUnexpectedErrors()
    {
        // Skip the log check if the scenario already failed for another reason
        if (_scenarioContext.TestError != null)
            return;

        var unexpected = AspireHook.LogCollector.GetUnexpectedErrors();
        if (unexpected.Count == 0) return;

        var messages = string.Join("\n",
            unexpected.Select(e => $"  [{e.Resource}] {e.Content}"));

        Assert.Fail(
            $"Scenario produced {unexpected.Count} unexpected error log(s):\n{messages}");
    }
}
