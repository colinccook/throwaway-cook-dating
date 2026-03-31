using CookDating.BddTests.Hooks;

namespace CookDating.BddTests.Support;

public static class TestConfiguration
{
    public static string BaseUrl => AspireHook.GetClientUrl();
    public static string BffUrl => AspireHook.GetBffUrl();
}
