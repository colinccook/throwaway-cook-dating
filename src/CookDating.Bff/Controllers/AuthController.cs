using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using CookDating.Bff.Dtos;
using CookDating.Bff.Handlers;
using CookDating.Bff.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CookDating.Bff.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class AuthController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly SignUpHandler _signUpHandler;
    private readonly CognitoSettings _cognitoSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAmazonCognitoIdentityProvider cognitoClient,
        SignUpHandler signUpHandler,
        CognitoSettings cognitoSettings,
        ILogger<AuthController> logger)
    {
        _cognitoClient = cognitoClient;
        _signUpHandler = signUpHandler;
        _cognitoSettings = cognitoSettings;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] Dtos.SignUpRequest request)
    {
        try
        {
            var response = await _signUpHandler.HandleAsync(request, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (EmailAlreadyRegisteredException)
        {
            LogSignUpEmailConflict(request.Email);
            return Conflict(new { message = "Email already registered" });
        }
        catch (Exception ex)
        {
            LogSignUpFailed(ex, request.Email);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
    {
        try
        {
            await _cognitoSettings.WaitUntilReadyAsync(HttpContext.RequestAborted);

            // Try Cognito auth first
            try
            {
                var authResponse = await _cognitoClient.InitiateAuthAsync(new InitiateAuthRequest
                {
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    ClientId = _cognitoSettings.ClientId,
                    AuthParameters = new Dictionary<string, string>
                    {
                        ["USERNAME"] = request.Email,
                        ["PASSWORD"] = request.Password
                    }
                });

                var userResponse = await _cognitoClient.GetUserAsync(new GetUserRequest
                {
                    AccessToken = authResponse.AuthenticationResult.AccessToken
                });

                return Ok(new AuthResponse(
                    authResponse.AuthenticationResult.AccessToken,
                    userResponse.UserAttributes.FirstOrDefault(a => a.Name == "sub")?.Value ?? userResponse.Username,
                    request.Email));
            }
            catch (NotAuthorizedException ex)
            {
                LogSignInInvalidCredentials(ex, request.Email);
                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (AmazonCognitoIdentityProviderException ex)
            {
                LogCognitoAuthUnavailable(ex, request.Email);
                var user = await _cognitoClient.AdminGetUserAsync(new AdminGetUserRequest
                {
                    UserPoolId = _cognitoSettings.UserPoolId,
                    Username = request.Email
                });

                var userId = user.UserAttributes.FirstOrDefault(a => a.Name == "sub")?.Value ?? user.Username;
                var token = PrototypeTokenHelper.GenerateJwt(userId, request.Email);
                return Ok(new AuthResponse(token, userId, request.Email));
            }
        }
        catch (UserNotFoundException)
        {
            LogSignInUserNotFound(request.Email);
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Sign-up rejected: email {Email} already registered")]
    private partial void LogSignUpEmailConflict(string email);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "Sign-up failed for {Email}")]
    private partial void LogSignUpFailed(Exception ex, string email);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Warning, Message = "Sign-in failed: invalid credentials for {Email}")]
    private partial void LogSignInInvalidCredentials(Exception ex, string email);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Warning, Message = "Cognito auth unavailable for {Email}, falling back to prototype JWT")]
    private partial void LogCognitoAuthUnavailable(Exception ex, string email);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Sign-in failed: user {Email} not found")]
    private partial void LogSignInUserNotFound(string email);
}
