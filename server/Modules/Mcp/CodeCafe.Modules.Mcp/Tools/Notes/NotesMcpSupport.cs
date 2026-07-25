using CodeCafe.Modules.Mcp.Common;
using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Identity;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

internal static class NotesMcpSupport
{
    internal readonly record struct NotebookContext(Guid ActorId, NotebookDetailModel Notebook);
    internal readonly record struct ItemContext(Guid ActorId, NotebookDetailModel Notebook, NotebookItemModel Item);
    internal readonly record struct NotebookSummaryContext(Guid ActorId, NotebookSummaryModel Notebook);
    internal readonly record struct ItemSummaryContext(Guid ActorId, NotebookSummaryModel Notebook, NotebookItemModel Item);

    internal static JsonSerializerOptions SerializerOptions => McpJson.SerializerOptions;
    private const string AuthenticatedActorRequiredCode = "authenticated_actor_required";
    private const string AuthenticatedActorRequiredMessage = "The MCP endpoint requires an authenticated CodeCafe user.";

    public static bool HasAnyScope(ClaimsPrincipal user, params string[] requiredScopes)
    {
        var scopeValues = user.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Concat(user.FindAll("scp")
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .ToHashSet(StringComparer.Ordinal);

        return requiredScopes.Any(scopeValues.Contains);
    }

    public static NotesResult RequireScope(ClaimsPrincipal user, params string[] requiredScopes)
    {
        return HasAnyScope(user, requiredScopes)
            ? NotesResult.Success()
            : NotesResult.Failure(
                NotesFailureKind.Forbidden,
                "insufficient_scope",
                $"The authenticated actor is missing required scope: {string.Join(" or ", requiredScopes)}.");
    }

    public static NotesResult<Guid> RequireActor(
        ClaimsPrincipal user,
        params string[] requiredScopes)
    {
        var scopeResult = RequireScope(user, requiredScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesResult<Guid>.Failure(scopeResult.Error!.Kind, scopeResult.Error.Code, scopeResult.Error.Message);
        }

        var currentUserId = CurrentUserClaims.GetUserId(user);
        return currentUserId is null
            ? NotesResult<Guid>.Failure(
                NotesFailureKind.Forbidden,
                AuthenticatedActorRequiredCode,
                AuthenticatedActorRequiredMessage)
            : NotesResult<Guid>.Success(currentUserId.Value);
    }

    public static string NormalizePath(string path) => path?.Trim().Trim('/') ?? string.Empty;

    private static IReadOnlyList<string> BuildPathLookupCandidates(string path, string? itemType = null)
    {
        var normalized = NormalizePath(path);
        var candidates = new List<string>();
        AddCandidate(candidates, normalized);

        var pathWithoutMcpType = StripKnownMcpItemTypePrefix(normalized, itemType);
        AddCandidate(candidates, pathWithoutMcpType);

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            AddCandidate(candidates, $"{itemType.Trim().ToLowerInvariant()}/{pathWithoutMcpType}");
        }
        else if (string.Equals(pathWithoutMcpType, normalized, StringComparison.Ordinal))
        {
            AddCandidate(candidates, $"page/{pathWithoutMcpType}");
            AddCandidate(candidates, $"folder/{pathWithoutMcpType}");
        }

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)
            || candidates.Contains(candidate, StringComparer.Ordinal))
        {
            return;
        }

        candidates.Add(candidate);
    }

    private static string StripKnownMcpItemTypePrefix(string path, string? itemType)
    {
        string[] prefixes = string.IsNullOrWhiteSpace(itemType)
            ? ["page/", "folder/"]
            : [$"{itemType.Trim().ToLowerInvariant()}/"];
        foreach (var prefix in prefixes)
        {
            if (path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return path[prefix.Length..];
            }
        }

        return path;
    }

    public static async Task<NotesResult<NotebookItemModel>> GetNotebookItemByMcpPathAsync(
        string notebookSlug,
        string path,
        Guid actorId,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        string? itemType = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_path",
                "The notebook item path is required.");
        }

        NotesError? firstError = null;
        foreach (var candidate in BuildPathLookupCandidates(path, itemType))
        {
            var itemResult = await notebookReadService.GetNotebookItemByPathAsync(
                notebookSlug,
                candidate,
                actorId,
                cancellationToken,
                includeArchived);

            if (itemResult.Succeeded)
            {
                return itemResult;
            }

            firstError ??= itemResult.Error;
        }

        return NotesResult<NotebookItemModel>.Failure(
            firstError?.Kind ?? NotesFailureKind.NotFound,
            firstError?.Code ?? "notebook_item_not_found",
            firstError?.Message ?? "Notebook item was not found.");
    }

    public static async Task<NotesResult<NotebookSummaryModel>> RequireNotebookAsync(
        string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserClaims.GetUserId(user);
        if (currentUserId is null)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                NotesFailureKind.Forbidden,
                AuthenticatedActorRequiredCode,
                AuthenticatedActorRequiredMessage);
        }

        if (string.IsNullOrWhiteSpace(notebookSlug))
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_slug",
                "The notebook slug is required.");
        }

        return await notebookReadService.GetNotebookSummaryBySlugAsync(notebookSlug.Trim(), currentUserId.Value, cancellationToken);
    }

    public static async Task<NotesResult<NotebookContext>> RequireNotebookContextAsync(
        string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes,
        bool includeArchived = false)
    {
        var actorResult = RequireActor(user, requiredScopes);
        if (!actorResult.Succeeded)
        {
            return NotesResult<NotebookContext>.Failure(actorResult.Error!.Kind, actorResult.Error.Code, actorResult.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(notebookSlug))
        {
            return NotesResult<NotebookContext>.Failure(
                NotesFailureKind.Validation,
                "invalid_slug",
                "The notebook slug is required.");
        }

        var notebookResult = await notebookReadService.GetNotebookBySlugAsync(
            notebookSlug.Trim(),
            actorResult.Value,
            cancellationToken,
            includeArchived);
        if (!notebookResult.Succeeded)
        {
            return NotesResult<NotebookContext>.Failure(
                notebookResult.Error!.Kind,
                notebookResult.Error.Code,
                notebookResult.Error.Message);
        }

        return NotesResult<NotebookContext>.Success(new NotebookContext(actorResult.Value, notebookResult.Value!));
    }

    public static async Task<NotesResult<NotebookSummaryContext>> RequireNotebookSummaryContextAsync(
        string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes,
        bool includeArchived = false)
    {
        var actorResult = RequireActor(user, requiredScopes);
        if (!actorResult.Succeeded)
        {
            return NotesResult<NotebookSummaryContext>.Failure(actorResult.Error!.Kind, actorResult.Error.Code, actorResult.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(notebookSlug))
        {
            return NotesResult<NotebookSummaryContext>.Failure(
                NotesFailureKind.Validation,
                "invalid_slug",
                "The notebook slug is required.");
        }

        var notebookResult = await notebookReadService.GetNotebookSummaryBySlugAsync(
            notebookSlug.Trim(),
            actorResult.Value,
            cancellationToken,
            includeArchived);
        if (!notebookResult.Succeeded)
        {
            return NotesResult<NotebookSummaryContext>.Failure(
                notebookResult.Error!.Kind,
                notebookResult.Error.Code,
                notebookResult.Error.Message);
        }

        return NotesResult<NotebookSummaryContext>.Success(new NotebookSummaryContext(actorResult.Value, notebookResult.Value!));
    }

    public static NotesResult<NotebookItemModel> RequireItem(
        NotebookDetailModel notebook,
        string path,
        string? itemType = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_path",
                "The notebook item path is required.");
        }

        NotebookItemModel? item = null;
        foreach (var candidate in BuildPathLookupCandidates(path, itemType))
        {
            item = notebook.Items.SingleOrDefault(existingItem =>
                string.Equals(existingItem.Path, candidate, StringComparison.Ordinal));
            if (item is not null)
            {
                break;
            }
        }

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(item);
    }

    public static NotesResult<NotebookItemModel> RequirePage(
        NotebookDetailModel notebook,
        string path)
    {
        var itemResult = RequireItem(notebook, path, "page");
        if (!itemResult.Succeeded)
        {
            return itemResult;
        }

        return string.Equals(itemResult.Value!.Type, "page", StringComparison.OrdinalIgnoreCase)
            ? itemResult
            : NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "page_required",
                "The requested notebook item is not a page.");
    }

    public static async Task<NotesResult<ItemContext>> RequireItemContextAsync(
        string notebookSlug,
        string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes,
        bool includeArchived = false,
        string? itemType = null)
    {
        var notebookContextResult = await RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookReadService,
            cancellationToken,
            requiredScopes,
            includeArchived);
        if (!notebookContextResult.Succeeded)
        {
            return NotesResult<ItemContext>.Failure(
                notebookContextResult.Error!.Kind,
                notebookContextResult.Error.Code,
                notebookContextResult.Error.Message);
        }

        var notebookContext = notebookContextResult.Value;
        var itemResult = RequireItem(notebookContext.Notebook, path, itemType);
        if (!itemResult.Succeeded)
        {
            return NotesResult<ItemContext>.Failure(
                itemResult.Error!.Kind,
                itemResult.Error.Code,
                itemResult.Error.Message);
        }

        return NotesResult<ItemContext>.Success(new ItemContext(notebookContext.ActorId, notebookContext.Notebook, itemResult.Value!));
    }

    public static async Task<NotesResult<ItemContext>> RequirePageContextAsync(
        string notebookSlug,
        string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes)
    {
        var notebookContextResult = await RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookReadService,
            cancellationToken,
            requiredScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesResult<ItemContext>.Failure(
                notebookContextResult.Error!.Kind,
                notebookContextResult.Error.Code,
                notebookContextResult.Error.Message);
        }

        var notebookContext = notebookContextResult.Value;
        var pageResult = RequirePage(notebookContext.Notebook, path);
        if (!pageResult.Succeeded)
        {
            return NotesResult<ItemContext>.Failure(
                pageResult.Error!.Kind,
                pageResult.Error.Code,
                pageResult.Error.Message);
        }

        return NotesResult<ItemContext>.Success(new ItemContext(notebookContext.ActorId, notebookContext.Notebook, pageResult.Value!));
    }

    public static async Task<NotesResult<ItemSummaryContext>> RequireItemSummaryContextAsync(
        string notebookSlug,
        string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes,
        bool includeArchived = false,
        string? itemType = null)
    {
        var notebookContextResult = await RequireNotebookSummaryContextAsync(
            notebookSlug,
            user,
            notebookReadService,
            cancellationToken,
            requiredScopes,
            includeArchived);
        if (!notebookContextResult.Succeeded)
        {
            return NotesResult<ItemSummaryContext>.Failure(
                notebookContextResult.Error!.Kind,
                notebookContextResult.Error.Code,
                notebookContextResult.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return NotesResult<ItemSummaryContext>.Failure(
                NotesFailureKind.Validation,
                "invalid_path",
                "The notebook item path is required.");
        }

        var notebookContext = notebookContextResult.Value;
        var itemResult = await GetNotebookItemByMcpPathAsync(
            notebookContext.Notebook.Slug,
            path,
            notebookContext.ActorId,
            notebookReadService,
            cancellationToken,
            includeArchived,
            itemType);
        if (!itemResult.Succeeded)
        {
            return NotesResult<ItemSummaryContext>.Failure(
                itemResult.Error!.Kind,
                itemResult.Error.Code,
                itemResult.Error.Message);
        }

        return NotesResult<ItemSummaryContext>.Success(new ItemSummaryContext(notebookContext.ActorId, notebookContext.Notebook, itemResult.Value!));
    }

    public static async Task<NotesResult<ItemSummaryContext>> RequirePageSummaryContextAsync(
        string notebookSlug,
        string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        string[] requiredScopes)
    {
        var itemContextResult = await RequireItemSummaryContextAsync(
            notebookSlug,
            path,
            user,
            notebookReadService,
            cancellationToken,
            requiredScopes,
            itemType: "page");
        if (!itemContextResult.Succeeded)
        {
            return itemContextResult;
        }

        return string.Equals(itemContextResult.Value.Item.Type, "page", StringComparison.OrdinalIgnoreCase)
            ? itemContextResult
            : NotesResult<ItemSummaryContext>.Failure(
                NotesFailureKind.Validation,
                "page_required",
                "The requested notebook item is not a page.");
    }

    public static NotesResult<NotebookItemModel> RequirePage(NotebookItemModel item)
    {
        return string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)
            ? NotesResult<NotebookItemModel>.Success(item)
            : NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "page_required",
                "The requested notebook item is not a page.");
    }

    public static async Task<NotesResult<NotebookItemModel?>> ResolveParentAsync(
        NotebookSummaryModel notebook,
        string? parentPath,
        Guid actorId,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return NotesResult<NotebookItemModel?>.Success(null);
        }

        var parentResult = await GetNotebookItemByMcpPathAsync(
            notebook.Slug,
            parentPath,
            actorId,
            notebookReadService,
            cancellationToken,
            includeArchived,
            itemType: "folder");
        if (!parentResult.Succeeded)
        {
            return NotesResult<NotebookItemModel?>.Failure(
                parentResult.Error!.Kind,
                parentResult.Error.Code,
                parentResult.Error.Message);
        }

        return string.Equals(parentResult.Value!.Type, "folder", StringComparison.OrdinalIgnoreCase)
            ? NotesResult<NotebookItemModel?>.Success(parentResult.Value)
            : NotesResult<NotebookItemModel?>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                "Parent item must be a folder.");
    }

    public static NotesResult<NotebookItemModel?> ResolveParent(
        NotebookDetailModel notebook,
        string? parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return NotesResult<NotebookItemModel?>.Success(null);
        }

        var parentResult = RequireItem(notebook, parentPath, "folder");
        if (!parentResult.Succeeded)
        {
            return NotesResult<NotebookItemModel?>.Failure(
                parentResult.Error!.Kind,
                parentResult.Error.Code,
                parentResult.Error.Message);
        }

        return string.Equals(parentResult.Value!.Type, "folder", StringComparison.OrdinalIgnoreCase)
            ? NotesResult<NotebookItemModel?>.Success(parentResult.Value)
            : NotesResult<NotebookItemModel?>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                "Parent item must be a folder.");
    }

    public static JsonElement SerializeToElement<T>(T value)
    {
        return McpJson.SerializeToElement(value);
    }

    public static string SerializeToJson<T>(T value)
    {
        return McpJson.Serialize(value);
    }

    public static NotebookItemSummaryToolResponse ToNotebookItemSummaryToolResponse(NotebookDetailModel notebook, NotebookItemModel item)
        => ToNotebookItemSummaryToolResponse(ToSummaryModel(notebook), item);

    public static NotebookItemSummaryToolResponse ToNotebookItemSummaryToolResponse(NotebookSummaryModel notebook, NotebookItemModel item)
    {
        return new NotebookItemSummaryToolResponse(
            item.Id,
            item.NotebookId,
            notebook.Slug,
            item.ParentId,
            item.Type,
            item.Title,
            item.Slug,
            item.Path,
            BuildItemResourceUri(notebook.Slug, item),
            item.SortOrder,
            item.ContentFormat,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public static GetNotebookToolResponse ToGetNotebookToolResponse(NotebookDetailModel notebook)
        => ToGetNotebookToolResponse(ToSummaryModel(notebook));

    public static GetNotebookToolResponse ToGetNotebookToolResponse(NotebookSummaryModel notebook)
    {
        return new GetNotebookToolResponse(
            notebook.Id,
            notebook.OwnerId,
            notebook.Slug,
            notebook.Title,
            notebook.Description,
            notebook.Visibility,
            notebook.IsPublished,
            notebook.AuthorDisplayName,
            notebook.CanEdit,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.FavoriteCount,
            notebook.IsFavoritedByMe,
            notebook.LastActivityAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc,
            BuildNotebookUri(notebook.Slug),
            BuildNotebookItemsUri(notebook.Slug));
    }

    public static GetPageToolResponse ToGetPageToolResponse(NotebookDetailModel notebook, NotebookItemModel page)
        => ToGetPageToolResponse(ToSummaryModel(notebook), page);

    public static GetPageToolResponse ToGetPageToolResponse(NotebookSummaryModel notebook, NotebookItemModel page)
    {
        var contentJson = SerializeJsonElement(page.ContentJson);
        var plainText = page.PlainTextContent;
        return new GetPageToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            BuildNotebookUri(notebook.Slug),
            BuildPageUri(notebook.Slug, page.Path),
            page.ContentFormat ?? "tiptap_json",
            contentJson,
            plainText,
            GetUtf8ByteCount(contentJson),
            plainText?.Length ?? 0,
            CountTipTapNodes(page.ContentJson),
            notebook.CanEdit,
            page.CreatedAtUtc,
            page.UpdatedAtUtc);
    }

    public static CreatePageToolResponse ToCreatePageToolResponse(NotebookDetailModel notebook, NotebookItemModel page, bool includeContent = false)
        => ToCreatePageToolResponse(ToSummaryModel(notebook), page, includeContent);

    public static CreatePageToolResponse ToCreatePageToolResponse(NotebookSummaryModel notebook, NotebookItemModel page, bool includeContent = false)
    {
        var contentJson = SerializeJsonElement(page.ContentJson);
        var plainText = page.PlainTextContent;
        return new CreatePageToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            BuildNotebookUri(notebook.Slug),
            BuildPageUri(notebook.Slug, page.Path),
            page.ParentId,
            page.SortOrder,
            page.ContentFormat,
            includeContent ? contentJson : null,
            includeContent ? plainText : null,
            includeContent,
            GetUtf8ByteCount(contentJson),
            plainText?.Length ?? 0,
            CountTipTapNodes(page.ContentJson),
            page.CreatedAtUtc,
            page.UpdatedAtUtc);
    }

    public static CreateItemToolResponse ToCreateItemToolResponse(NotebookDetailModel notebook, NotebookItemModel item)
        => ToCreateItemToolResponse(ToSummaryModel(notebook), item);

    public static CreateItemToolResponse ToCreateItemToolResponse(NotebookSummaryModel notebook, NotebookItemModel item)
    {
        return new CreateItemToolResponse(
            item.Id,
            notebook.Id,
            notebook.Slug,
            item.Title,
            item.Type,
            item.Path,
            BuildNotebookUri(notebook.Slug),
            BuildNotebookItemsUri(notebook.Slug),
            BuildItemResourceUri(notebook.Slug, item),
            item.ParentId,
            item.SortOrder,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public static UpdatePageContentToolResponse ToUpdatePageContentToolResponse(NotebookDetailModel notebook, NotebookItemModel page, bool includeContent = false)
        => ToUpdatePageContentToolResponse(ToSummaryModel(notebook), page, includeContent);

    public static UpdatePageContentToolResponse ToUpdatePageContentToolResponse(NotebookSummaryModel notebook, NotebookItemModel page, bool includeContent = false)
    {
        var contentJson = SerializeJsonElement(page.ContentJson);
        var plainText = page.PlainTextContent;
        return new UpdatePageContentToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            BuildNotebookUri(notebook.Slug),
            BuildPageUri(notebook.Slug, page.Path),
            page.ContentFormat,
            includeContent ? contentJson : null,
            includeContent ? plainText : null,
            includeContent,
            GetUtf8ByteCount(contentJson),
            plainText?.Length ?? 0,
            CountTipTapNodes(page.ContentJson),
            page.UpdatedAtUtc);
    }

    public static string? SerializeJsonElement(JsonElement? value)
    {
        if (!value.HasValue || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.Value.GetRawText();
    }

    private static int GetUtf8ByteCount(string? value)
        => string.IsNullOrEmpty(value) ? 0 : Encoding.UTF8.GetByteCount(value);

    private static int CountTipTapNodes(JsonElement? contentJson)
    {
        if (!contentJson.HasValue || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return 0;
        }

        return CountTipTapNodes(contentJson.Value);
    }

    private static int CountTipTapNodes(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        var count = 1;
        if (node.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in contentElement.EnumerateArray())
            {
                count += CountTipTapNodes(child);
            }
        }

        return count;
    }

    public static MoveItemToolResponse ToMoveItemToolResponse(NotebookDetailModel notebook, NotebookItemModel item)
        => ToMoveItemToolResponse(ToSummaryModel(notebook), item);

    public static MoveItemToolResponse ToMoveItemToolResponse(NotebookSummaryModel notebook, NotebookItemModel item)
    {
        return new MoveItemToolResponse(
            item.Id,
            notebook.Id,
            notebook.Slug,
            item.Title,
            item.Type,
            item.Path,
            BuildNotebookUri(notebook.Slug),
            BuildNotebookItemsUri(notebook.Slug),
            BuildItemResourceUri(notebook.Slug, item),
            item.ParentId,
            item.SortOrder,
            item.UpdatedAtUtc);
    }

    public static bool MatchesNotebook(NotebookDetailModel notebook, string query)
        => MatchesNotebook(ToSummaryModel(notebook), query);

    public static bool MatchesNotebook(NotebookSummaryModel notebook, string query)
    {
        return notebook.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
               || (notebook.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public static string? BuildPlainTextSnippet(string? plainText, string query)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        var index = plainText.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return plainText.Length <= 160 ? plainText : plainText[..160];
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(160, plainText.Length - start);
        return plainText.Substring(start, length);
    }

    public static JsonElement AppendBlocks(JsonElement? existingContentJson, JsonElement blocks)
        => TipTapDocumentOperations.AppendBlocks(existingContentJson, blocks);

    public static JsonElement ReplaceBlockAtIndex(JsonElement? existingContentJson, int index, JsonElement block)
        => TipTapDocumentOperations.ReplaceBlockAtIndex(existingContentJson, index, block);

    public static JsonElement InsertBlocksAtIndex(JsonElement? existingContentJson, int index, JsonElement blocks)
        => TipTapDocumentOperations.InsertBlocksAtIndex(existingContentJson, index, blocks);

    public static JsonElement DeleteBlockAtIndex(JsonElement? existingContentJson, int index)
        => TipTapDocumentOperations.DeleteBlockAtIndex(existingContentJson, index);

    public static JsonElement ReplaceTextInDocument(JsonElement? existingContentJson, string searchText, string replacementText, bool replaceAll)
        => TipTapDocumentOperations.ReplaceTextInDocument(existingContentJson, searchText, replacementText, replaceAll);

    public static string BuildNotebookUri(string notebookSlug) => $"notebook://{notebookSlug}";

    public static string BuildNotebookItemsUri(string notebookSlug) => $"notebook://{notebookSlug}/items";

    public static string BuildPageUri(string notebookSlug, string path) => $"page://{notebookSlug}/{NormalizePath(path)}";

    public static string? BuildItemResourceUri(string notebookSlug, NotebookItemModel item)
    {
        return string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase)
            ? BuildPageUri(notebookSlug, item.Path)
            : null;
    }

    public static JsonElement ToGuidJsonElement(Guid? value)
    {
        return value is null
            ? JsonSerializer.SerializeToElement<string?>(null, SerializerOptions)
            : JsonSerializer.SerializeToElement(value.Value.ToString(), SerializerOptions);
    }

    public static IEnumerable<ChatMessage> CreatePromptMessages(params string[] messages)
    {
        return messages.Select(message => new ChatMessage(ChatRole.User, message));
    }

    private static NotebookSummaryModel ToSummaryModel(NotebookDetailModel notebook)
    {
        return new NotebookSummaryModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.Visibility,
            notebook.IsPublished,
            notebook.AuthorDisplayName,
            notebook.CanEdit,
            notebook.ItemCount,
            notebook.FolderCount,
            notebook.PageCount,
            notebook.FavoriteCount,
            notebook.IsFavoritedByMe,
            notebook.LastActivityAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc);
    }

    public static void EnsureMcpSuccess(NotesResult result)
    {
        if (!result.Succeeded)
        {
            ThrowMcpError(result.Error!);
        }
    }

    public static T EnsureMcpSuccess<T>(NotesResult<T> result)
    {
        if (!result.Succeeded)
        {
            ThrowMcpError(result.Error!);
        }

        return result.Value!;
    }

    public static void ThrowMcpError(NotesError error)
    {
        throw new McpException($"{error.Code}: {error.Message}");
    }
}
