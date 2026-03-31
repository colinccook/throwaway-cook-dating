using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
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
public class AuthController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly ProfileCommandHandlers _profileHandlers;
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly CognitoSettings _cognitoSettings;

    public AuthController(
        IAmazonCognitoIdentityProvider cognitoClient,
        ProfileCommandHandlers profileHandlers,
        MatchingCommandHandlers matchingHandlers,
        CognitoSettings cognitoSettings)
    {
        _cognitoClient = cognitoClient;
        _profileHandlers = profileHandlers;
        _matchingHandlers = matchingHandlers;
        _cognitoSettings = cognitoSettings;
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
            catch (AmazonCognitoIdentityProviderException)
            {
                // floci may not support AdminConfirmSignUp
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
            catch (NotAuthorizedException)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
            catch (AmazonCognitoIdentityProviderException)
            {
                // Cognito auth not available (e.g. user not confirmed in floci) —
                // look up the user and issue a prototype JWT
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
        catch
        {
            // Cognito auth not available — generate a prototype JWT
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
}
