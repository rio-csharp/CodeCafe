using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;

namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

public sealed class GetNotebookDetailsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetNotebookDetailsQuery, Result<NotebookDetailsDto>>
{
    public async Task<Result<NotebookDetailsDto>> Handle(GetNotebookDetailsQuery query, CancellationToken cancellationToken)
    {
        var notebook = await notebooks.FindByIdOrSlugAsync(query.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            return Result.Failure<NotebookDetailsDto>(NotebookErrors.NotFound);
        }

        var userId = currentUserAccessor.User?.Id;
        var denied = NotebookAccess.CheckRead(
            notebook,
            userId,
            query.AccessCode,
            passwordHasher
        );
        if (denied is not null)
        {
            // A page-share recipient reaches the notebook through the shared subtree (the tree
            // already exposes exactly that subset), so the details shell must open for them too;
            // otherwise the shared page link lands on "notebook not found".
            var hasSharedPage = userId is { } sharedUserId
                && await pages.HasSharedPageAsync(notebook.Id, sharedUserId, cancellationToken);
            if (!hasSharedPage)
            {
                return Result.Failure<NotebookDetailsDto>(denied);
            }
        }

        var dto = await NotebookDetailsMapping.ToDtoAsync(
            notebook,
            userId,
            users,
            cancellationToken
        );
        var pageCount = await pages.CountByNotebookAsync(notebook.Id, cancellationToken);

        return Result.Success(dto with { PageCount = pageCount });
    }
}
