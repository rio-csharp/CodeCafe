using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Shared;

public static class NotebookDetailsMapping
{
    // includeShares gates the collaborator list: it identifies people, so only the owner gets
    // it — public notebooks are readable anonymously and must not leak their member list.
    public static async Task<NotebookDetailsDto> ToDtoAsync(
        Notebook notebook,
        bool includeShares,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var shareUserIds = includeShares
            ? notebook.Shares.Select(share => share.UserId).ToList()
            : [];
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
            // Shares whose user no longer exists are omitted rather than rendered with a
            // placeholder name; an empty name map therefore means "no shares visible".
            notebook.Shares
                .Where(share => shareUserNames.ContainsKey(share.UserId))
                .Select(share => new NotebookShareDto(
                    share.UserId,
                    shareUserNames[share.UserId],
                    share.Role
                ))
                .ToList(),
            PageCount: 0,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc
        );
}
