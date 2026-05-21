using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Interfaces;
using Microsoft.Extensions.AI;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.WebApi.Mcp;

internal static class NotesMcpSupport
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }

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

    public static string NormalizePath(string path) => path.Trim().Trim('/');

    public static async Task<NotesResult<NotebookDetailModel>> RequireNotebookAsync(
        string notebookSlug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId(user);
        if (currentUserId == Guid.Empty)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Forbidden,
                "authenticated_actor_required",
                "The MCP endpoint requires an authenticated CodeCafe user.");
        }

        if (string.IsNullOrWhiteSpace(notebookSlug))
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_slug",
                "The notebook slug is required.");
        }

        return await notebookQueryService.GetNotebookBySlugAsync(notebookSlug.Trim(), currentUserId, cancellationToken);
    }

    public static NotesResult<NotebookItemModel> RequireItem(
        NotebookDetailModel notebook,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_path",
                "The notebook item path is required.");
        }

        var normalizedPath = NormalizePath(path);
        var item = notebook.Items.SingleOrDefault(existingItem =>
            string.Equals(existingItem.Path, normalizedPath, StringComparison.Ordinal));

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
        var itemResult = RequireItem(notebook, path);
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

    public static NotesResult<NotebookItemModel> ResolveParent(
        NotebookDetailModel notebook,
        string? parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return NotesResult<NotebookItemModel>.Success(null!);
        }

        var parentResult = RequireItem(notebook, parentPath);
        if (!parentResult.Succeeded)
        {
            return parentResult;
        }

        return string.Equals(parentResult.Value!.Type, "folder", StringComparison.OrdinalIgnoreCase)
            ? parentResult
            : NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                "Parent item must be a folder.");
    }

    public static JsonElement SerializeToElement<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, SerializerOptions);
    }

    public static string SerializeToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    public static NotebookItemToolResponse ToNotebookItemToolResponse(NotebookItemModel item)
    {
        return new NotebookItemToolResponse(
            item.Id,
            item.NotebookId,
            item.ParentId,
            item.Type,
            item.Title,
            item.Slug,
            item.Path,
            item.SortOrder,
            item.ContentFormat,
            item.ContentJson,
            item.PlainTextContent,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public static GetPageToolResponse ToGetPageToolResponse(NotebookDetailModel notebook, NotebookItemModel page)
    {
        return new GetPageToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            page.ContentFormat ?? "tiptap_json",
            page.ContentJson,
            page.PlainTextContent,
            notebook.CanEdit,
            page.CreatedAtUtc,
            page.UpdatedAtUtc);
    }

    public static CreatePageToolResponse ToCreatePageToolResponse(NotebookDetailModel notebook, NotebookItemModel page)
    {
        return new CreatePageToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            page.ParentId,
            page.SortOrder,
            page.ContentFormat,
            page.ContentJson,
            page.PlainTextContent,
            page.CreatedAtUtc,
            page.UpdatedAtUtc);
    }

    public static UpdatePageContentToolResponse ToUpdatePageContentToolResponse(NotebookDetailModel notebook, NotebookItemModel page)
    {
        return new UpdatePageContentToolResponse(
            page.Id,
            notebook.Id,
            notebook.Slug,
            page.Title,
            page.Path,
            page.ContentFormat,
            page.ContentJson,
            page.PlainTextContent,
            page.UpdatedAtUtc);
    }

    public static MoveItemToolResponse ToMoveItemToolResponse(NotebookDetailModel notebook, NotebookItemModel item)
    {
        return new MoveItemToolResponse(
            item.Id,
            notebook.Id,
            notebook.Slug,
            item.Title,
            item.Type,
            item.Path,
            item.ParentId,
            item.SortOrder,
            item.UpdatedAtUtc);
    }

    public static bool MatchesNotebook(NotebookDetailModel notebook, string query)
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
    {
        if (blocks.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Blocks must be a JSON array.", nameof(blocks));
        }

        var root = existingContentJson is null || existingContentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? new JsonObject
            {
                ["type"] = "doc",
                ["content"] = new JsonArray()
            }
            : JsonNode.Parse(existingContentJson.Value.GetRawText())?.AsObject()
              ?? new JsonObject
              {
                  ["type"] = "doc",
                  ["content"] = new JsonArray()
              };

        root["type"] ??= "doc";
        var content = root["content"] as JsonArray ?? new JsonArray();
        root["content"] = content;

        foreach (var block in blocks.EnumerateArray())
        {
            content.Add(JsonNode.Parse(block.GetRawText()));
        }

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
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

    public static async Task AuditWriteAsync(
        IMcpAuditService auditService,
        ClaimsPrincipal user,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        NotesResult result,
        CancellationToken cancellationToken)
    {
        await auditService.WriteAsync(
            GetCurrentUserId(user),
            "user",
            toolName,
            notebookId,
            itemId,
            result.Succeeded,
            result.Succeeded ? "success" : result.Error!.Code,
            result.Error?.Code,
            cancellationToken);
    }

    public static async Task AuditWriteAsync<T>(
        IMcpAuditService auditService,
        ClaimsPrincipal user,
        string toolName,
        Guid? notebookId,
        Guid? itemId,
        NotesResult<T> result,
        CancellationToken cancellationToken)
    {
        await auditService.WriteAsync(
            GetCurrentUserId(user),
            "user",
            toolName,
            notebookId,
            itemId,
            result.Succeeded,
            result.Succeeded ? "success" : result.Error!.Code,
            result.Error?.Code,
            cancellationToken);
    }
}
