using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed class SetNotebookTagsCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetNotebookTagsCommand, Result>
{
    public async Task<Result> Handle(SetNotebookTagsCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindByIdOrSlugAsync(command.NotebookIdOrSlug, cancellationToken)
            : null;
        if (notebook is null || notebook.OwnerId != userId)
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        // Lowercase like slugs: filtering by tag becomes case-insensitive for free.
        var tags = command.Tags
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Where(tag => tag.Length > 0)
            .Distinct()
            .Take(Notebook.MaxTagCount)
            .ToList();

        notebook.SetTags(tags);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
