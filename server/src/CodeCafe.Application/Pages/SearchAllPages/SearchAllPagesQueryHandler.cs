using System.Globalization;
using System.Text;
using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.SearchAllPages;

public sealed class SearchAllPagesQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IPageRepository pages
) : IQueryHandler<SearchAllPagesQuery, Result<CursorPage<PageSearchHitDto>>>
{
    internal const int DefaultPageSize = 20;
    internal const int MaxPageSize = 50;

    public async Task<Result<CursorPage<PageSearchHitDto>>> Handle(SearchAllPagesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<CursorPage<PageSearchHitDto>>(AuthErrors.UserNotFound);
        }

        DateTimeOffset? cursorUpdatedAtUtc = null;
        Guid? cursorId = null;
        if (query.Cursor is not null)
        {
            if (!TryDecodeCursor(query.Cursor, out var decodedUpdatedAtUtc, out var decodedId))
            {
                return Result.Failure<CursorPage<PageSearchHitDto>>(PageErrors.InvalidCursor);
            }

            cursorUpdatedAtUtc = decodedUpdatedAtUtc;
            cursorId = decodedId;
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var trimmedQuery = query.Query.Trim();

        // One extra row answers "is there a next page" without a count query.
        var matches = await pages.SearchAsync(
            userId.Value,
            trimmedQuery,
            cursorUpdatedAtUtc,
            cursorId,
            pageSize + 1,
            cancellationToken
        );

        var page = matches.Take(pageSize).ToList();
        var items = new List<PageSearchHitDto>(page.Count);
        foreach (var match in page)
        {
            items.Add(await BuildHitAsync(match, trimmedQuery, cancellationToken));
        }

        // The cursor format is opaque to callers: base64 of "{updatedAtUtc:o}|{id}". It stays
        // local to this handler because nothing else encodes keyset cursors yet.
        var nextCursor = matches.Count > pageSize && page.Count > 0
            ? EncodeCursor(page[^1].Page.UpdatedAtUtc, page[^1].Page.Id)
            : null;

        return Result.Success(new CursorPage<PageSearchHitDto>(items, nextCursor));
    }

    // Paths are derived from the parent chain, so each hit walks its ancestors; the page size
    // cap (50) and shallow trees keep the extra reads bounded. Title-only matches carry an empty
    // snippet: the DTO's Snippet is non-nullable, and fetching a first-block preview would cost
    // a second round trip per hit.
    private async Task<PageSearchHitDto> BuildHitAsync(PageSearchMatch match, string query, CancellationToken cancellationToken)
    {
        var ancestors = await PageHierarchy.LoadAncestorsAsync(match.Page, pages, cancellationToken);
        return new PageSearchHitDto(
            match.Page.Id,
            match.Page.NotebookId,
            match.NotebookSlug,
            match.NotebookTitle,
            match.Page.Title,
            PageHierarchy.PathOf(match.Page, ancestors),
            match.MatchedBlockPlainText is null
                ? string.Empty
                : SearchSnippet.Build(match.MatchedBlockPlainText, query)
        );
    }

    private static string EncodeCursor(DateTimeOffset updatedAtUtc, Guid id)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{updatedAtUtc:o}|{id}"));

    private static bool TryDecodeCursor(string cursor, out DateTimeOffset updatedAtUtc, out Guid id)
    {
        updatedAtUtc = default;
        id = default;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = decoded.Split('|');
        return parts.Length == 2
            && DateTimeOffset.TryParseExact(
                parts[0],
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out updatedAtUtc
            )
            && Guid.TryParse(parts[1], out id);
    }
}
