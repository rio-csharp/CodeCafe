using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.PreparePageImport;

// Checks the target once, then hands the client a short-lived grant. The upload endpoint
// re-checks access when the token is used, so a permission change in between still wins.
public sealed class PreparePageImportCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages
) : ICommandHandler<PreparePageImportCommand, Result<ImportUploadGrant>>
{
    public async Task<Result<ImportUploadGrant>> Handle(
        PreparePageImportCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.MaxBytes <= 0)
        {
            return Result.Failure<ImportUploadGrant>(ImportUploadErrors.InvalidMaxBytes);
        }

        // The model may pass a Windows path regardless of the server OS, so normalize both
        // separators before taking the basename used for the page-title fallback.
        var fileName = Path.GetFileName(command.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<ImportUploadGrant>(PageErrors.InvalidImport);
        }

        var access = await NotebookAccess.RequireWriterAsync(
            command.NotebookIdOrSlug,
            currentUserAccessor,
            notebooks,
            cancellationToken
        );
        if (access.Error is { } error)
        {
            return Result.Failure<ImportUploadGrant>(error);
        }

        var context = access.Value!;
        string? parentPath = null;
        if (!string.IsNullOrWhiteSpace(command.ParentPath))
        {
            var segments = PagePath.Parse(command.ParentPath);
            if (segments.Count > 0)
            {
                parentPath = PagePath.Build(segments);
                if (await PageHierarchy.ResolveByPathAsync(context.Notebook.Id, parentPath, pages, cancellationToken) is null)
                {
                    return Result.Failure<ImportUploadGrant>(PageErrors.ParentNotFound);
                }
            }
        }

        return Result.Success(
            new ImportUploadGrant(
                ImportUploadKind.Page,
                context.UserId,
                context.Notebook.Id,
                parentPath,
                fileName,
                command.MaxBytes
            )
        );
    }
}
