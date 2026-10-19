using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Notebooks.PrepareNotebookImport;

public sealed class PrepareNotebookImportCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users
) : ICommandHandler<PrepareNotebookImportCommand, Result<ImportUploadGrant>>
{
    public async Task<Result<ImportUploadGrant>> Handle(
        PrepareNotebookImportCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.MaxBytes <= 0)
        {
            return Result.Failure<ImportUploadGrant>(ImportUploadErrors.InvalidMaxBytes);
        }

        var fileName = Path.GetFileName(command.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<ImportUploadGrant>(NotebookErrors.InvalidImport);
        }

        // Importing a notebook creates one, so the only authority to check is that the caller
        // still exists. The notebook itself does not exist yet.
        var resolved = await CurrentUserResolver.RequireAsync(currentUserAccessor, users, cancellationToken);
        if (resolved.Error is { } error)
        {
            return Result.Failure<ImportUploadGrant>(error);
        }

        return Result.Success(
            new ImportUploadGrant(
                ImportUploadKind.Notebook,
                resolved.Value!.Id,
                null,
                null,
                fileName,
                command.MaxBytes
            )
        );
    }
}
