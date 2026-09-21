using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Shared;

public static class NotebookDetailsMapping
{
    public static async Task<NotebookDetailsDto> ToDtoAsync(
        Notebook notebook,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var shareUserIds = notebook.Shares.Select(share => share.UserId).ToList();
        var shareUsers = shareUserIds.Count == 0
            ? (IReadOnlyList<CodeCafe.Domain.Identity.User>)[]
            : await users.FindByIdsAsync(shareUserIds, cancellationToken);
        var namesById = shareUsers.ToDictionary(user => user.Id, user => user.DisplayName);

        return ToDto(notebook, namesById);
    }

    public static NotebookDetailsDto ToDto(Notebook notebook, IReadOnlyDictionary<Guid, string> shareUserNames)
        => new(
            notebook.Id,
            notebook.Title,
            notebook.Description,
            notebook.Slug,
            notebook.Visibility,
            notebook.AccessCodeHash is not null,
            notebook.Tags.ToList(),
            notebook.Shares
                .Select(share => new NotebookShareDto(
                    share.UserId,
                    shareUserNames.GetValueOrDefault(share.UserId, "unknown"),
                    share.Role
                ))
                .ToList(),
            PageCount: 0,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc
        );
}
