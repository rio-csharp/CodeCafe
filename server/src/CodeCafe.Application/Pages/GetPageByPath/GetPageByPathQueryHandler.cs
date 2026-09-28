using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPageByPath;

public sealed class GetPageByPathQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetPageByPathQuery, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(GetPageByPathQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = await notebooks.FindByIdOrSlugAsync(query.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            return Result.Failure<PageDetailsDto>(NotebookErrors.NotFound);
        }

        var page = await PageHierarchy.ResolveByPathAsync(notebook.Id, query.Path, pages, cancellationToken);
        if (page is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.NotFound);
        }

        return await PageReadProjection.ToDetailsAsync(
            notebook,
            page,
            userId,
            query.AccessCode,
            pages,
            users,
            passwordHasher,
            cancellationToken
        );
    }
}
