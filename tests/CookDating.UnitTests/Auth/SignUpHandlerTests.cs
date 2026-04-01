using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using CookDating.Bff.Dtos;
using CookDating.Bff.Handlers;
using CookDating.Bff.Infrastructure;
using CookDating.Matching.Application.Handlers;
using CookDating.Matching.Domain;
using CookDating.Profile.Application.Handlers;
using CookDating.Profile.Domain;
using CookDating.SharedKernel.Domain;
using Microsoft.Extensions.Logging;
using Moq;
using DomainMatch = CookDating.Matching.Domain.Match;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using CognitoSignUpRequest = Amazon.CognitoIdentityProvider.Model.SignUpRequest;

namespace CookDating.UnitTests.Auth;

[TestFixture]
public class SignUpHandlerTests
{
    [Test]
    public async Task HandleAsync_ValidSignUp_CompletesFullFlow()
    {
        var ctx = CreateTestContext();
        SetupCognitoSignUp(ctx, "user-123");
        SetupCognitoConfirm(ctx);
        SetupCognitoAuthFallback(ctx);
        var sut = CreateHandler(ctx);

        var result = await sut.HandleAsync(BuildRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.UserId, Is.EqualTo("user-123"));
            Assert.That(result.Email, Is.EqualTo("test@example.com"));
            Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
        });
        // Profile and matching candidate should have been created
        Assert.That(ctx.ProfileRepository.SaveCallCount, Is.EqualTo(1));
        Assert.That(ctx.CandidateRepository.SaveCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_InvalidDob_CreatesReservationButFailsProfile()
    {
        var ctx = CreateTestContext();
        SetupCognitoSignUp(ctx, "user-123");
        var sut = CreateHandler(ctx);

        var request = BuildRequest(dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)).ToString("yyyy-MM-dd"));

        var ex = Assert.ThrowsAsync<ArgumentException>(() => sut.HandleAsync(request));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.Contain("Must be at least 18 years old"));
            // Cognito SignUp was called (reservation created)
            ctx.CognitoMock.Verify(c => c.SignUpAsync(
                It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            // AdminConfirmSignUp should NOT have been called (profile failed first)
            ctx.CognitoMock.Verify(c => c.AdminConfirmSignUpAsync(
                It.IsAny<AdminConfirmSignUpRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            // No profile or matching candidate saved
            Assert.That(ctx.ProfileRepository.SaveCallCount, Is.Zero);
            Assert.That(ctx.CandidateRepository.SaveCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task HandleAsync_RetryOverridesReservationWhenNoProfileExists()
    {
        var ctx = CreateTestContext();

        // First SignUp throws UsernameExistsException (reservation exists)
        ctx.CognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { UserSub = "user-new" });
        ctx.CognitoMock
            .SetupSequence(c => c.SignUpAsync(It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameExistsException("exists"))
            .ReturnsAsync(new SignUpResponse { UserSub = "user-new" });

        // AdminGetUser returns existing reservation
        ctx.CognitoMock
            .Setup(c => c.AdminGetUserAsync(It.IsAny<AdminGetUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminGetUserResponse
            {
                Username = "test@example.com",
                UserAttributes = [new AttributeType { Name = "sub", Value = "user-old" }]
            });

        // No existing profile (it's a reservation)
        ctx.ProfileRepository.SetGetByIdResult(null);

        // AdminDeleteUser succeeds
        ctx.CognitoMock
            .Setup(c => c.AdminDeleteUserAsync(It.IsAny<AdminDeleteUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminDeleteUserResponse());

        SetupCognitoConfirm(ctx);
        SetupCognitoAuthFallback(ctx);
        var sut = CreateHandler(ctx);

        var result = await sut.HandleAsync(BuildRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.UserId, Is.EqualTo("user-new"));
            Assert.That(ctx.ProfileRepository.SaveCallCount, Is.EqualTo(1));
            ctx.CognitoMock.Verify(c => c.AdminDeleteUserAsync(
                It.IsAny<AdminDeleteUserRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public void HandleAsync_ThrowsWhenConfirmedProfileExists()
    {
        var ctx = CreateTestContext();

        // SignUp throws UsernameExistsException
        ctx.CognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameExistsException("exists"));

        // AdminGetUser returns existing user
        ctx.CognitoMock
            .Setup(c => c.AdminGetUserAsync(It.IsAny<AdminGetUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminGetUserResponse
            {
                Username = "test@example.com",
                UserAttributes = [new AttributeType { Name = "sub", Value = "user-existing" }]
            });

        // Profile EXISTS — this is a confirmed account, not a reservation
        ctx.ProfileRepository.SetGetByIdResult(
            UserProfile.Create("user-existing", "Existing User",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Gender.Male,
                new DatingPreferences(Gender.Female, 18, 35, 50)));

        var sut = CreateHandler(ctx);

        var ex = Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => sut.HandleAsync(BuildRequest()));

        Assert.That(ex!.Email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task HandleAsync_AdminDeleteUserUnsupported_ReusesExistingUserId()
    {
        var ctx = CreateTestContext();

        // SignUp throws UsernameExistsException
        ctx.CognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameExistsException("exists"));

        // AdminGetUser returns existing reservation
        ctx.CognitoMock
            .Setup(c => c.AdminGetUserAsync(It.IsAny<AdminGetUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminGetUserResponse
            {
                Username = "test@example.com",
                UserAttributes = [new AttributeType { Name = "sub", Value = "user-reused" }]
            });

        // No profile (it's a reservation)
        ctx.ProfileRepository.SetGetByIdResult(null);

        // AdminDeleteUser NOT supported (throws)
        ctx.CognitoMock
            .Setup(c => c.AdminDeleteUserAsync(It.IsAny<AdminDeleteUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonCognitoIdentityProviderException("Operation AdminDeleteUser is not supported"));

        SetupCognitoConfirm(ctx);
        SetupCognitoAuthFallback(ctx);
        var sut = CreateHandler(ctx);

        var result = await sut.HandleAsync(BuildRequest());

        Assert.Multiple(() =>
        {
            // Reused the existing user ID since delete wasn't available
            Assert.That(result.UserId, Is.EqualTo("user-reused"));
            Assert.That(ctx.ProfileRepository.SaveCallCount, Is.EqualTo(1));
        });
    }

    private static SignUpHandler CreateHandler(TestContext ctx)
    {
        var profileHandlers = new ProfileCommandHandlers(
            ctx.ProfileRepository,
            ctx.EventPublisher);
        var matchingHandlers = new MatchingCommandHandlers(
            ctx.CandidateRepository,
            ctx.MatchRepository,
            ctx.EventPublisher);

        return new SignUpHandler(
            ctx.CognitoMock.Object,
            profileHandlers,
            matchingHandlers,
            ctx.ProfileRepository,
            InitializedCognitoSettings(),
            new SilentLogger<SignUpHandler>());
    }

    private static TestContext CreateTestContext()
        => new(
            new Mock<IAmazonCognitoIdentityProvider>(),
            new RecordingProfileRepository(),
            new RecordingCandidateRepository(),
            new RecordingMatchRepository(),
            new RecordingEventPublisher());

    private static void SetupCognitoSignUp(TestContext ctx, string userSub)
    {
        ctx.CognitoMock
            .Setup(c => c.SignUpAsync(It.IsAny<CognitoSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse { UserSub = userSub });
    }

    private static void SetupCognitoConfirm(TestContext ctx)
    {
        ctx.CognitoMock
            .Setup(c => c.AdminConfirmSignUpAsync(
                It.IsAny<AdminConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminConfirmSignUpResponse());
    }

    private static void SetupCognitoAuthFallback(TestContext ctx)
    {
        // Auth will fall back to prototype JWT
        ctx.CognitoMock
            .Setup(c => c.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonCognitoIdentityProviderException("not supported"));
    }

    private static CookDating.Bff.Dtos.SignUpRequest BuildRequest(
        string dateOfBirth = "1995-06-15",
        int minAge = 18,
        int maxAge = 35)
        => new(
            Email: "test@example.com",
            Password: "TestPass123!",
            DisplayName: "Test User",
            DateOfBirth: dateOfBirth,
            Gender: "Male",
            PreferredGender: "Female",
            MinAge: minAge,
            MaxAge: maxAge,
            MaxDistanceKm: 50);

    private static CognitoSettings InitializedCognitoSettings()
    {
        var settings = new CognitoSettings();
        settings.Initialize("pool-id", "client-id");
        return settings;
    }

    private sealed record TestContext(
        Mock<IAmazonCognitoIdentityProvider> CognitoMock,
        RecordingProfileRepository ProfileRepository,
        RecordingCandidateRepository CandidateRepository,
        RecordingMatchRepository MatchRepository,
        RecordingEventPublisher EventPublisher);

    private sealed class SilentLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    private sealed class RecordingEventPublisher : IEventPublisher
    {
        public int SinglePublishCallCount { get; private set; }
        public int BatchPublishCallCount { get; private set; }

        public Task PublishAsync(IDomainEvent domainEvent, string topicArn, CancellationToken cancellationToken = default)
        {
            SinglePublishCallCount++;
            return Task.CompletedTask;
        }

        public Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, string topicArn, CancellationToken cancellationToken = default)
        {
            BatchPublishCallCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingProfileRepository : IProfileRepository
    {
        public int GetByIdCallCount { get; private set; }
        public int SaveCallCount { get; private set; }
        private UserProfile? _getByIdResult;

        public void SetGetByIdResult(UserProfile? result) => _getByIdResult = result;

        public Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(_getByIdResult);
        }

        public Task SaveAsync(UserProfile aggregate, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingCandidateRepository : IMatchCandidateRepository
    {
        public int SaveCallCount { get; private set; }

        public Task<MatchCandidate?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<MatchCandidate?>(null);

        public Task SaveAsync(MatchCandidate aggregate, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<MatchCandidate>> GetActiveCandidatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<List<MatchCandidate>>([]);
    }

    private sealed class RecordingMatchRepository : IMatchRepository
    {
        public Task<DomainMatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<DomainMatch?>(null);

        public Task SaveAsync(DomainMatch aggregate, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<DomainMatch>> GetMatchesForUserAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<List<DomainMatch>>([]);

        public Task<DomainMatch?> GetMatchBetweenUsersAsync(string userId1, string userId2, CancellationToken cancellationToken = default)
            => Task.FromResult<DomainMatch?>(null);
    }
}
