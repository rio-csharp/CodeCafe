using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IAuthUserGateway authUserGateway)
    : IQueryHandler<GetCurrentUserQuery, AuthResult<AuthUserModel>>
{
    public async Task<AuthResult<AuthUserModel>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var user = await authUserGateway.FindByIdAsync(request.UserId, cancellationToken);
        return user is null ? Unauthorized() : AuthResult<AuthUserModel>.Success(user);
    }

    private static AuthResult<AuthUserModel> Unauthorized()
    {
        return AuthResult<AuthUserModel>.Failure(
            AuthFailureKind.Unauthorized,
            "unauthorized",
            "Authentication is required."
        );
    }
}
