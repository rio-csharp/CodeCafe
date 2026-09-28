using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPage;

public sealed class GetPageQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetPageQuery, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(GetPageQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var page = await pages.FindByIdAsync(query.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.NotFound);
        }

        // A live page in a trashed notebook must not be readable.
        var notebook = await notebooks.FindByIdAsync(page.NotebookId, cancellationToken);
        if (notebook is null)
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
