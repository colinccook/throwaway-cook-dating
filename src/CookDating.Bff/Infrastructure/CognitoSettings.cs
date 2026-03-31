namespace CookDating.Bff.Infrastructure;

/// <summary>
/// Holds dynamically-created Cognito user pool and client IDs.
/// Populated by <see cref="CognitoBootstrapHostedService"/> at startup.
/// </summary>
public class CognitoSettings
{
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string UserPoolId { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;

    public void Initialize(string userPoolId, string clientId)
    {
        UserPoolId = userPoolId;
        ClientId = clientId;
        _readyTcs.TrySetResult();
    }

    public Task WaitUntilReadyAsync(CancellationToken ct = default)
        => _readyTcs.Task.WaitAsync(ct);
}
