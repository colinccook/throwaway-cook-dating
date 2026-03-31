using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using CookDating.Bff.Dtos;
using CookDating.Bff.Infrastructure;
using CookDating.Matching.Application.Commands;
using CookDating.Matching.Application.Handlers;
using CookDating.Profile.Application.Commands;
using CookDating.Profile.Application.Handlers;
using CookDating.Profile.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using CognitoSignUpRequest = Amazon.CognitoIdentityProvider.Model.SignUpRequest;

namespace CookDating.Bff.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class AuthController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly ProfileCommandHandlers _profileHandlers;
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly CognitoSettings _cognitoSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAmazonCognitoIdentityProvider cognitoClient,
        ProfileCommandHandlers profileHandlers,
        MatchingCommandHandlers matchingHandlers,
        CognitoSettings cognitoSettings,
        ILogger<AuthController> logger)
    {
        _cognitoClient = cognitoClient;
        _profileHandlers = profileHandlers;
        _matchingHandlers = matchingHandlers;
        _cognitoSettings = cognitoSettings;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] Dtos.SignUpRequest request)
    {
        try
        {
            await _cognitoSettings.WaitUntilReadyAsync(HttpContext.RequestAborted);

            var signUpResponse = await _cognitoClient.SignUpAsync(new CognitoSignUpRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = request.Email,
                Password = request.Password,
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = request.Email }
                ]
            });

            var userId = signUpResponse.UserSub;

            // Try to confirm the user — skip if the provider doesn't support it
            try
            {
                await _cognitoClient.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest
                {
                    UserPoolId = _cognitoSettings.UserPoolId,
                    Username = request.Email
                });
            }
            catch (AmazonCognitoIdentityProviderException ex)
            {
                LogAdminConfirmSignUpUnavailable(ex, request.Email);
            }

            // Create profile in Profile BC
            var gender = Enum.Parse<Gender>(request.Gender);
            Gender? preferredGender = request.PreferredGender != null
                ? Enum.Parse<Gender>(request.PreferredGender)
                : null;

            await _profileHandlers.HandleAsync(new CreateProfileCommand(
                userId, request.DisplayName, DateOnly.Parse(request.DateOfBirth),
                gender, preferredGender, request.MinAge, request.MaxAge, request.MaxDistanceKm));

            // Sync candidate directly (avoids waiting for async worker processing)
            await _matchingHandlers.HandleAsync(new ProcessProfileCreatedCommand(
                userId, request.DisplayName, request.Gender,
                request.PreferredGender, request.MinAge, request.MaxAge, request.MaxDistanceKm));

            // Get an access token — try Cognito first, fall back to a local JWT
            // (the BFF accepts any JWT in prototype mode)
            var accessToken = await GetAccessTokenAsync(request.Email, request.Password, userId);

            return Ok(new AuthResponse(accessToken, userId, request.Email));
        }
        catch (UsernameExistsException)
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
                    userResponse.UserAttributes.First(a => a.Name == "sub").Value,
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

                var userId = user.UserAttributes.First(a => a.Name == "sub").Value;
                var token = GeneratePrototypeJwt(userId, request.Email);
                return Ok(new AuthResponse(token, userId, request.Email));
            }
        }
        catch (UserNotFoundException)
        {
            LogSignInUserNotFound(request.Email);
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    private async Task<string> GetAccessTokenAsync(string email, string password, string userId)
    {
        try
        {
            var authResponse = await _cognitoClient.InitiateAuthAsync(new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _cognitoSettings.ClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    ["USERNAME"] = email,
                    ["PASSWORD"] = password
                }
            });
            return authResponse.AuthenticationResult.AccessToken;
        }
        catch (Exception ex)
        {
            LogCognitoTokenFallback(ex, email);
            return GeneratePrototypeJwt(userId, email);
        }
    }

    private static string GeneratePrototypeJwt(string userId, string email)
    {
        var key = new SymmetricSecurityKey(
            "prototype-key-not-for-production-use-1234567890"u8.ToArray());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("sub", userId),
                new Claim("email", email)
            ],
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "AdminConfirmSignUp not available for {Email}, skipping confirmation")]
    private partial void LogAdminConfirmSignUpUnavailable(Exception ex, string email);

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

    [LoggerMessage(EventId = 3007, Level = LogLevel.Warning, Message = "Cognito token acquisition failed for {Email}, using prototype JWT")]
    private partial void LogCognitoTokenFallback(Exception ex, string email);
}
