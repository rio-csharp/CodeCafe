using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

public sealed class GetNotebookDetailsQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
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

        var denied = NotebookReadAccess.Check(
            notebook,
            currentUserAccessor.User?.Id,
            query.AccessCode,
            passwordHasher
        );
        if (denied is not null)
        {
            return Result.Failure<NotebookDetailsDto>(denied);
        }

        return Result.Success(await NotebookDetailsMapping.ToDtoAsync(notebook, users, cancellationToken));
    }
}
