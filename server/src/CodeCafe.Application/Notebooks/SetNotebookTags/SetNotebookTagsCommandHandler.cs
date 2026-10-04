using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.SetNotebookTags;

public sealed class SetNotebookTagsCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetNotebookTagsCommand, Result>
{
    public async Task<Result> Handle(SetNotebookTagsCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireOwnerAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;

        // Lowercase like slugs: filtering by tag becomes case-insensitive for free.
        // The validator caps the raw count at MaxTagCount, so deduping can only shrink it.
        var tags = command.Tags
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Where(tag => tag.Length > 0)
            .Distinct()
            .ToList();

        notebook.SetTags(tags);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
