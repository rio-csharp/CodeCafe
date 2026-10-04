using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.UpdateNotebook;

public sealed class UpdateNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateNotebookCommand, Result<NotebookDetailsDto>>
{
    public async Task<Result<NotebookDetailsDto>> Handle(UpdateNotebookCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireOwnerAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<NotebookDetailsDto>(error);
        }

        var notebook = context.Value!.Notebook;

        // Null fields keep their current values; an explicit blank description clears it.
        // The validator rejects blank titles, so a non-null title is always worth trimming.
        var title = command.Title?.Trim() ?? notebook.Title;
        var description = command.Description is null
            ? notebook.Description
            : command.Description.Trim() is { Length: > 0 } newDescription ? newDescription : null;

        notebook.UpdateDetails(title, description, command.Visibility ?? notebook.Visibility);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await NotebookDetailsMapping.ToDtoAsync(notebook, includeShares: true, users, cancellationToken));
    }
}
