using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Notebooks.DeleteNotebook;

public sealed class DeleteNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeleteNotebookCommand, Result>
{
    public async Task<Result> Handle(DeleteNotebookCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindByIdOrSlugAsync(command.NotebookIdOrSlug, cancellationToken)
            : null;
        if (notebook is null || notebook.OwnerId != userId)
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        notebook.SoftDelete(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
