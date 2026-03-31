using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace CookDating.Bff.Infrastructure;

/// <summary>
/// Creates the Cognito user pool and app client in floci at startup,
/// then publishes the generated IDs into <see cref="CognitoSettings"/>.
/// </summary>
public class CognitoBootstrapHostedService(
    IAmazonCognitoIdentityProvider cognitoClient,
    CognitoSettings settings,
    ILogger<CognitoBootstrapHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Cognito bootstrapping attempt {Attempt}/{MaxRetries}…", attempt, maxRetries);

                var poolResponse = await cognitoClient.CreateUserPoolAsync(new CreateUserPoolRequest
                {
                    PoolName = "cook-dating-pool",
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
                        ClientName = "cook-dating-client",
                        ExplicitAuthFlows = ["ALLOW_USER_PASSWORD_AUTH", "ALLOW_REFRESH_TOKEN_AUTH"]
                    }, stoppingToken);

                var clientId = clientResponse.UserPoolClient.ClientId;

                settings.Initialize(userPoolId, clientId);

                logger.LogInformation(
                    "Cognito bootstrapping completed: UserPoolId={UserPoolId}, ClientId={ClientId}",
                    userPoolId, clientId);
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                logger.LogWarning(ex,
                    "Cognito bootstrapping attempt {Attempt} failed, retrying in {Delay}s…",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
