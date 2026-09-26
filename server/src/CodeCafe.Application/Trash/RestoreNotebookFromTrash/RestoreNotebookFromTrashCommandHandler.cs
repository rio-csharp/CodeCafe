using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Trash.RestoreNotebookFromTrash;

public sealed class RestoreNotebookFromTrashCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<RestoreNotebookFromTrashCommand, Result>
{
    public async Task<Result> Handle(RestoreNotebookFromTrashCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindTrashedByIdAsync(command.NotebookId, cancellationToken)
            : null;

        // Trashed notebooks of others are invisible here, same as anywhere else.
        if (notebook is null || notebook.OwnerId != userId)
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        // The slug may have been taken while the notebook sat in the trash; rather than fail
        // the restore, the notebook comes back with a fresh suffixed slug.
        if (await notebooks.FindBySlugAsync(notebook.Slug, cancellationToken) is not null)
        {
            var candidates = await SlugAvailability.FindAvailableAsync(
                notebook.Slug,
                Notebook.MaxSlugLength,
                1,
                async (candidate, ct) => await notebooks.FindBySlugAsync(candidate, ct) is not null,
                cancellationToken
            );
            if (candidates.Count > 0)
            {
                notebook.ChangeSlug(candidates[0]);
            }
        }

        notebook.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
