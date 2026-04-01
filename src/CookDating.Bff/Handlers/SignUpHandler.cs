using System.Globalization;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CookDating.Bff.Dtos;
using CookDating.Bff.Infrastructure;
using CookDating.Matching.Application.Commands;
using CookDating.Matching.Application.Handlers;
using CookDating.Profile.Application.Commands;
using CookDating.Profile.Application.Handlers;
using CookDating.Profile.Domain;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using CognitoSignUpRequest = Amazon.CognitoIdentityProvider.Model.SignUpRequest;

namespace CookDating.Bff.Handlers;

public partial class SignUpHandler
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly ProfileCommandHandlers _profileHandlers;
    private readonly MatchingCommandHandlers _matchingHandlers;
    private readonly IProfileRepository _profileRepository;
    private readonly CognitoSettings _cognitoSettings;
    private readonly ILogger<SignUpHandler> _logger;

    public SignUpHandler(
        IAmazonCognitoIdentityProvider cognitoClient,
        ProfileCommandHandlers profileHandlers,
        MatchingCommandHandlers matchingHandlers,
        IProfileRepository profileRepository,
        CognitoSettings cognitoSettings,
        ILogger<SignUpHandler> logger)
    {
        _cognitoClient = cognitoClient;
        _profileHandlers = profileHandlers;
        _matchingHandlers = matchingHandlers;
        _profileRepository = profileRepository;
        _cognitoSettings = cognitoSettings;
        _logger = logger;
    }

    public async Task<AuthResponse> HandleAsync(Dtos.SignUpRequest request, CancellationToken ct = default)
    {
        await _cognitoSettings.WaitUntilReadyAsync(ct);

        // 1. Reserve a Cognito user (or reclaim an existing reservation)
        var userId = await ReserveOrReclaimCognitoUserAsync(request, ct);

        // 2. Create the profile — domain validates DOB, preferences, etc.
        var dateOfBirth = ParseDateOfBirth(request.DateOfBirth);
        var gender = Enum.Parse<Gender>(request.Gender);
        Gender? preferredGender = request.PreferredGender is not null
            ? Enum.Parse<Gender>(request.PreferredGender)
            : null;

        await _profileHandlers.HandleAsync(new CreateProfileCommand(
            userId, request.DisplayName, dateOfBirth,
            gender, preferredGender, request.MinAge, request.MaxAge, request.MaxDistanceKm), ct);

        // 3. Confirm the Cognito reservation (skip if provider doesn't support it)
        try
        {
            await _cognitoClient.AdminConfirmSignUpAsync(new AdminConfirmSignUpRequest
            {
                UserPoolId = _cognitoSettings.UserPoolId,
                Username = request.Email
            }, ct);
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            LogAdminConfirmSignUpUnavailable(ex, request.Email);
        }

        // 4. Sync candidate for matching
        await _matchingHandlers.HandleAsync(new ProcessProfileCreatedCommand(
            userId, request.DisplayName, request.Gender,
            request.PreferredGender, request.MinAge, request.MaxAge, request.MaxDistanceKm), ct);

        // 5. Get access token
        var accessToken = await GetAccessTokenAsync(request.Email, request.Password, userId, ct);

        return new AuthResponse(accessToken, userId, request.Email);
    }

    private async Task<string> ReserveOrReclaimCognitoUserAsync(Dtos.SignUpRequest request, CancellationToken ct)
    {
        try
        {
            var signUpResponse = await _cognitoClient.SignUpAsync(new CognitoSignUpRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = request.Email,
                Password = request.Password,
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = request.Email }
                ]
            }, ct);

            return signUpResponse.UserSub;
        }
        catch (UsernameExistsException)
        {
            return await ReclaimReservationAsync(request, ct);
        }
    }

    private async Task<string> ReclaimReservationAsync(Dtos.SignUpRequest request, CancellationToken ct)
    {
        // Check if this is just an unfinished reservation (no profile) or a real account
        var existingUser = await _cognitoClient.AdminGetUserAsync(new AdminGetUserRequest
        {
            UserPoolId = _cognitoSettings.UserPoolId,
            Username = request.Email
        }, ct);

        var existingUserId = existingUser.UserAttributes
            .FirstOrDefault(a => a.Name == "sub")?.Value ?? existingUser.Username;

        var existingProfile = await _profileRepository.GetByIdAsync(existingUserId, ct);
        if (existingProfile is not null)
            throw new EmailAlreadyRegisteredException(request.Email);

        // No profile means this is a reservation — try to delete and re-create
        LogReclaimingReservation(request.Email);

        try
        {
            await _cognitoClient.AdminDeleteUserAsync(new AdminDeleteUserRequest
            {
                UserPoolId = _cognitoSettings.UserPoolId,
                Username = request.Email
            }, ct);

            var signUpResponse = await _cognitoClient.SignUpAsync(new CognitoSignUpRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = request.Email,
                Password = request.Password,
                UserAttributes =
                [
                    new AttributeType { Name = "email", Value = request.Email }
                ]
            }, ct);

            return signUpResponse.UserSub;
        }
        catch (AmazonCognitoIdentityProviderException ex)
        {
            // AdminDeleteUser not supported (e.g. floci) — reuse existing user
            LogAdminDeleteUserUnavailable(ex, request.Email);
            return existingUserId;
        }
    }

    private static DateOnly ParseDateOfBirth(string dateOfBirth)
    {
        if (!DateOnly.TryParseExact(dateOfBirth, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            throw new ArgumentException(
                "Date of birth must be a valid date in yyyy-MM-dd format",
                nameof(dateOfBirth));

        return parsed;
    }

    private async Task<string> GetAccessTokenAsync(string email, string password, string userId, CancellationToken ct)
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
            }, ct);
            return authResponse.AuthenticationResult.AccessToken;
        }
        catch (Exception ex)
        {
            LogCognitoTokenFallback(ex, email);
            return PrototypeTokenHelper.GenerateJwt(userId, email);
        }
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "AdminConfirmSignUp not available for {Email}, skipping confirmation")]
    private partial void LogAdminConfirmSignUpUnavailable(Exception ex, string email);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Reclaiming Cognito reservation for {Email} (no profile found)")]
    private partial void LogReclaimingReservation(string email);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "AdminDeleteUser not available for {Email}, reusing existing Cognito user")]
    private partial void LogAdminDeleteUserUnavailable(Exception ex, string email);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Warning, Message = "Cognito token acquisition failed for {Email}, using prototype JWT")]
    private partial void LogCognitoTokenFallback(Exception ex, string email);
}
