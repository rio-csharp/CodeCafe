using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

public static class PageDetailsMapping
{
    public static async Task<PageDetailsDto> ToDtoAsync(
        Page page,
        string path,
        bool isFavorite,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var shareUserIds = page.Shares.Select(share => share.UserId).ToList();
        var shareUsers = shareUserIds.Count == 0
            ? (IReadOnlyList<CodeCafe.Domain.Identity.User>)[]
            : await users.FindByIdsAsync(shareUserIds, cancellationToken);
        var namesById = shareUsers.ToDictionary(user => user.Id, user => user.DisplayName);

        return ToDto(page, path, isFavorite, namesById);
    }

    public static PageDetailsDto ToDto(Page page, string path, bool isFavorite, IReadOnlyDictionary<Guid, string> shareUserNames)
        => new(
            page.Id,
            page.NotebookId,
            page.Title,
            path,
            page.IsArchived,
            isFavorite,
            page.Shares
                .Select(share => new PageShareDto(
                    share.UserId,
                    shareUserNames.GetValueOrDefault(share.UserId, "unknown"),
                    share.Role
                ))
                .ToList(),
            // Blocks land with the blocks slice; pages report empty content until then.
            Blocks: [],
            page.CreatedAtUtc,
            page.UpdatedAtUtc
        );
}
