using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Logout;

public sealed class LogoutCommandHandler(
    IUnitOfWork unitOfWork,
    IRefreshTokenService refreshTokens
) : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeAsync(command.RefreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
