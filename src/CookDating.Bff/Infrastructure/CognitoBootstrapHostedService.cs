using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CookDating.Bff.Infrastructure;

/// <summary>
/// Creates the Cognito user pool and app client in floci at startup,
/// then publishes the generated IDs into <see cref="CognitoSettings"/>.
/// </summary>
public partial class CognitoBootstrapHostedService(
    IAmazonCognitoIdentityProvider cognitoClient,
    CognitoSettings settings,
    IConfiguration configuration,
    ILogger<CognitoBootstrapHostedService> logger) : BackgroundService
{
    private readonly string _tenantId = configuration["TENANT_ID"] ?? "cook-dating";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                LogBootstrapAttempt(logger, attempt, maxRetries);

                var poolResponse = await cognitoClient.CreateUserPoolAsync(new CreateUserPoolRequest
                {
                    PoolName = $"{_tenantId}-pool",
                    AutoVerifiedAttributes = ["email"],
                    Policies = new UserPoolPolicyType
                    {
                        PasswordPolicy = new PasswordPolicyType
                        {
                            MinimumLength = 8,
                            RequireUppercase = false,
                            RequireLowercase = false,
                            RequireNumbers = false,
                            RequireSymbols = false
                        }
                    }
                }, stoppingToken);

                var userPoolId = poolResponse.UserPool.Id;

                var clientResponse = await cognitoClient.CreateUserPoolClientAsync(
                    new CreateUserPoolClientRequest
                    {
                        UserPoolId = userPoolId,
                        ClientName = $"{_tenantId}-client",
                        ExplicitAuthFlows = ["ALLOW_USER_PASSWORD_AUTH", "ALLOW_REFRESH_TOKEN_AUTH"]
                    }, stoppingToken);

                var clientId = clientResponse.UserPoolClient.ClientId;

                settings.Initialize(userPoolId, clientId);

                LogBootstrapCompleted(logger, userPoolId, clientId);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                LogBootstrapRetry(logger, ex, attempt, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    [LoggerMessage(EventId = 7001, Level = LogLevel.Information, Message = "Cognito bootstrapping attempt {Attempt}/{MaxRetries}…")]
    private static partial void LogBootstrapAttempt(ILogger logger, int attempt, int maxRetries);

    [LoggerMessage(EventId = 7002, Level = LogLevel.Information, Message = "Cognito bootstrapping completed: UserPoolId={UserPoolId}, ClientId={ClientId}")]
    private static partial void LogBootstrapCompleted(ILogger logger, string userPoolId, string clientId);

    [LoggerMessage(EventId = 7003, Level = LogLevel.Warning, Message = "Cognito bootstrapping attempt {Attempt} failed, retrying in {Delay}s…")]
    private static partial void LogBootstrapRetry(ILogger logger, Exception ex, int attempt, double delay);
}
