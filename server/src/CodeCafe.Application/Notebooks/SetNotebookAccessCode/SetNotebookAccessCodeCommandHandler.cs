using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.SetNotebookAccessCode;

public sealed class SetNotebookAccessCodeCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher
) : ICommandHandler<SetNotebookAccessCodeCommand, Result>
{
    public async Task<Result> Handle(SetNotebookAccessCodeCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireOwnerAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;

        // Null clears the code; only the hash is ever stored.
        var hash = command.AccessCode is not null ? passwordHasher.Hash(command.AccessCode) : null;
        notebook.SetAccessCodeHash(hash);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
