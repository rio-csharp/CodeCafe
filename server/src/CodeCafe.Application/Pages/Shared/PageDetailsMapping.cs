using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

public static class PageDetailsMapping
{
    // includeShares gates the collaborator list: it identifies people, so only the notebook
    // owner gets it — public pages are readable anonymously and must not leak the member list.
    public static async Task<PageDetailsDto> ToDtoAsync(
        Page page,
        string path,
        bool isFavorite,
        bool includeShares,
        bool canWrite,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var shareUserIds = includeShares
            ? page.Shares.Select(share => share.UserId).ToList()
            : [];
        var shareUsers = shareUserIds.Count == 0
            ? (IReadOnlyList<CodeCafe.Domain.Identity.User>)[]
            : await users.FindByIdsAsync(shareUserIds, cancellationToken);
        var namesById = shareUsers.ToDictionary(user => user.Id, user => user.DisplayName);

        return ToDto(page, path, isFavorite, canWrite, namesById);
    }

    public static PageDetailsDto ToDto(
        Page page,
        string path,
        bool isFavorite,
        bool canWrite,
        IReadOnlyDictionary<Guid, string> shareUserNames
    )
        => new(
            page.Id,
            page.NotebookId,
            page.Title,
            path,
            page.IsArchived,
            isFavorite,
            // Shares whose user no longer exists are omitted rather than rendered with a
            // placeholder name; an empty name map therefore means "no shares visible".
            page.Shares
                .Where(share => shareUserNames.ContainsKey(share.UserId))
                .Select(share => new PageShareDto(
                    share.UserId,
                    shareUserNames[share.UserId],
                    share.Role
                ))
                .ToList(),
            // The read projection fills blocks after the page/share DTO is mapped.
            Blocks: [],
            page.CreatedAtUtc,
            page.UpdatedAtUtc,
            canWrite
        );
}
