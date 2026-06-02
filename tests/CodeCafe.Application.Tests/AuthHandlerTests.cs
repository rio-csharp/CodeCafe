using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Commands.AuthenticateUser;
using CodeCafe.Application.Auth.Commands.RegisterUser;
using CodeCafe.Application.Auth.Queries.GetCurrentUser;

namespace CodeCafe.Application.Tests;

public sealed class AuthHandlerTests
{
    [Fact]
    public async Task RegisterUserHandler_ReturnsForbidden_WhenRegistrationDisabled()
    {
        var gateway = new StubAuthUserGateway();
        var handler = new RegisterUserCommandHandler(gateway);

        var result = await handler.Handle(
            new RegisterUserCommand(false, "new.user@example.com", "Password123!", "New User"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureKind.Forbidden, result.Error?.Kind);
        Assert.False(gateway.CreateUserCalled);
    }

    [Fact]
    public async Task RegisterUserHandler_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var gateway = new StubAuthUserGateway
        {
            ExistingUser = new AuthUserModel(Guid.NewGuid(), "existing.user@example.com", "Existing User")
        };
        var handler = new RegisterUserCommandHandler(gateway);

        var result = await handler.Handle(
            new RegisterUserCommand(true, "existing.user@example.com", "Password123!", "Existing User"),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureKind.Conflict, result.Error?.Kind);
        Assert.False(gateway.CreateUserCalled);
    }

    [Fact]
    public async Task AuthenticateUserHandler_ReturnsUser_WhenCredentialsAreValid()
    {
        var existingUser = new AuthUserModel(Guid.NewGuid(), "yao@example.com", "Yao");
        var gateway = new StubAuthUserGateway
        {
            ExistingUser = existingUser,
            PasswordVerificationResult = AuthPasswordVerificationResult.Success()
        };
        var handler = new AuthenticateUserCommandHandler(gateway);

        var result = await handler.Handle(
            new AuthenticateUserCommand("yao@example.com", "Password123!"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(existingUser, result.Value);
        Assert.True(gateway.VerifyPasswordCalled);
    }

    [Fact]
    public async Task GetCurrentUserHandler_ReturnsUnauthorized_WhenUserCannotBeFound()
    {
        var handler = new GetCurrentUserQueryHandler(new StubAuthUserGateway());

        var result = await handler.Handle(
            new GetCurrentUserQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureKind.Unauthorized, result.Error?.Kind);
    }

    private sealed class StubAuthUserGateway : IAuthUserGateway
    {
        public AuthUserModel? ExistingUser { get; init; }

        public AuthUserModel? UserById { get; init; }

        public AuthCreateUserResult CreateUserResult { get; init; } =
            AuthCreateUserResult.Success(new AuthUserModel(Guid.NewGuid(), "created@example.com", "Created User"));

        public AuthPasswordVerificationResult PasswordVerificationResult { get; init; } =
            AuthPasswordVerificationResult.Failure(isLockedOut: false);

        public bool CreateUserCalled { get; private set; }

        public bool VerifyPasswordCalled { get; private set; }

        public Task<AuthUserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(ExistingUser);

        public Task<AuthUserModel?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(UserById);

        public Task<AuthCreateUserResult> CreateUserAsync(
            string normalizedEmail,
            string displayName,
            string password,
            CancellationToken cancellationToken)
        {
            CreateUserCalled = true;
            return Task.FromResult(CreateUserResult);
        }

        public Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
            Guid userId,
            string password,
            bool lockoutOnFailure,
            CancellationToken cancellationToken)
        {
            VerifyPasswordCalled = true;
            return Task.FromResult(PasswordVerificationResult);
        }
    }
}
