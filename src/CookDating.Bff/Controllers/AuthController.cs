using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CookDating.Bff.Dtos;
using CookDating.Profile.Application.Commands;
using CookDating.Profile.Application.Handlers;
using CookDating.Profile.Domain;
using Microsoft.AspNetCore.Mvc;
using CognitoSignUpRequest = Amazon.CognitoIdentityProvider.Model.SignUpRequest;

namespace CookDating.Bff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly ProfileCommandHandlers _profileHandlers;
    private const string UserPoolId = "us-east-1_testpool";
    private const string ClientId = "test-client-id";

    public AuthController(IAmazonCognitoIdentityProvider cognitoClient, ProfileCommandHandlers profileHandlers)
    {
        _cognitoClient = cognitoClient;
        _profileHandlers = profileHandlers;
    }

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> SignUp([FromBody] Dtos.SignUpRequest request)
    {
        try
        {
            var signUpResponse = await _cognitoClient.SignUpAsync(new CognitoSignUpRequest
            {
                ClientId = ClientId,
                Username = request.Email,
                Password = request.Password,
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = request.Email }
                ]
            });

            var userId = signUpResponse.UserSub;

            // Auto-confirm the user (for prototype simplicity)
            await _cognitoClient.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest
            {
                UserPoolId = UserPoolId,
                Username = request.Email
            });

            // Create profile in Profile BC
            var gender = Enum.Parse<Gender>(request.Gender);
            Gender? preferredGender = request.PreferredGender != null
                ? Enum.Parse<Gender>(request.PreferredGender)
                : null;

            await _profileHandlers.HandleAsync(new CreateProfileCommand(
                userId, request.DisplayName, DateOnly.Parse(request.DateOfBirth),
                gender, preferredGender, request.MinAge, request.MaxAge, request.MaxDistanceKm));

            // Sign in to get tokens
            var authResponse = await _cognitoClient.InitiateAuthAsync(new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = ClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    ["USERNAME"] = request.Email,
                    ["PASSWORD"] = request.Password
                }
            });

            return Ok(new AuthResponse(
                authResponse.AuthenticationResult.AccessToken,
                userId,
                request.Email));
        }
        catch (UsernameExistsException)
        {
            return Conflict(new { message = "Email already registered" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
    {
        try
        {
            var authResponse = await _cognitoClient.InitiateAuthAsync(new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = ClientId,
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
        catch (NotAuthorizedException)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }
}
