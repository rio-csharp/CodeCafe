using System.Text;
using System.Text.Json;
using CodeCafe.Modules.Ai.Configuration;
using CodeCafe.Modules.Notes.Application.Notes;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace CodeCafe.Modules.Ai.Edits;

public sealed class OpenAiNotebookEditGenerator(
    OpenAIClient openAiClient,
    IOptions<AiOptions> aiOptionsAccessor,
    ITipTapPlainTextExtractor plainTextExtractor) : IAiNotebookEditGenerator
{
    private const int ActivePagePreviewChars = 2400;
    private const int ActivePageJsonChars = 12000;
    private const int BlockSnippetChars = 220;
    private const int ItemPreviewChars = 600;
    private const string FullDocumentMode = "full_document";
    private const string OperationsMode = "operations";
    private static readonly HashSet<string> SupportedOperations =
    [
        "replace_current_page",
        "append_to_current_page",
        "create_page",
        "delete_page",
    ];

    private readonly AiOptions _options = aiOptionsAccessor.Value;

    public async Task<AiNotebookEditResult> GenerateEditAsync(
        AiNotebookEditGenerationContext context,
        CancellationToken cancellationToken)
    {
        var client = openAiClient.GetChatClient(_options.Model);
        var completionResult = await client.CompleteChatAsync(
            [
                new SystemChatMessage(EditInstructions),
                new UserChatMessage(BuildUserPrompt(context))
            ],
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = Math.Max(1, _options.MaxDraftOutputTokens),
                EndUserId = context.CurrentUserId.ToString("N")
            },
            cancellationToken);

        var responseText = string.Concat(
            completionResult.Value.Content
                .Where(part => !string.IsNullOrEmpty(part.Text))
                .Select(part => part.Text));

        return ParseResponse(responseText, context);
    }

    private AiNotebookEditResult ParseResponse(string responseText, AiNotebookEditGenerationContext context)
    {
        var normalized = StripCodeFence(responseText).Trim();
        using var document = JsonDocument.Parse(normalized);
        var root = document.RootElement;

        var operation = root.TryGetProperty("action", out var actionProperty) && actionProperty.ValueKind == JsonValueKind.String
            ? actionProperty.GetString()?.Trim().ToLowerInvariant()
            : null;
        if (string.IsNullOrWhiteSpace(operation) || !SupportedOperations.Contains(operation))
        {
            throw new InvalidOperationException("The assistant returned an invalid action.");
        }

        var title = root.TryGetProperty("title", out var titleProperty) && titleProperty.ValueKind == JsonValueKind.String
            ? titleProperty.GetString()?.Trim()
            : null;
        var summary = root.TryGetProperty("summary", out var summaryProperty) && summaryProperty.ValueKind == JsonValueKind.String
            ? summaryProperty.GetString()?.Trim()
            : null;

        if (operation == "delete_page")
        {
            return new AiNotebookEditResult(
                operation,
                OperationsMode,
                string.IsNullOrWhiteSpace(title) ? GetFallbackTitle(operation, context) : title,
                string.IsNullOrWhiteSpace(summary) ? GetFallbackSummary(operation, context) : summary,
                null,
                null);
        }

        var mode = root.TryGetProperty("mode", out var modeProperty) && modeProperty.ValueKind == JsonValueKind.String
            ? modeProperty.GetString()?.Trim().ToLowerInvariant()
            : null;

        JsonElement? contentJson = null;
        JsonElement? operationsJson = null;

        if (root.TryGetProperty("contentJson", out var contentJsonProperty)
            && contentJsonProperty.ValueKind == JsonValueKind.Object)
        {
            contentJson = contentJsonProperty.Clone();
            mode ??= FullDocumentMode;
        }

        if (root.TryGetProperty("operations", out var operationsProperty)
            && operationsProperty.ValueKind == JsonValueKind.Array)
        {
            operationsJson = operationsProperty.Clone();
            mode ??= OperationsMode;
        }

        if (mode is not FullDocumentMode and not OperationsMode)
        {
            throw new InvalidOperationException("The assistant returned an invalid mode.");
        }

        if (mode == FullDocumentMode && contentJson is null)
        {
            throw new InvalidOperationException("The assistant returned full_document mode without contentJson.");
        }

        if (mode == OperationsMode && operationsJson is null)
        {
            throw new InvalidOperationException("The assistant returned operations mode without operations.");
        }

        return new AiNotebookEditResult(
            operation,
            mode,
            string.IsNullOrWhiteSpace(title) ? GetFallbackTitle(operation, context) : title,
            string.IsNullOrWhiteSpace(summary) ? GetFallbackSummary(operation, context) : summary,
            contentJson,
            operationsJson);
    }

    private string BuildUserPrompt(AiNotebookEditGenerationContext context)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a notebook edit proposal for CodeCafe.");
        prompt.AppendLine($"Locale: {context.Locale}");
        prompt.AppendLine($"Requested operation: {context.RequestedOperation}");
        prompt.AppendLine();
        prompt.AppendLine("User request:");
        prompt.AppendLine(TrimForPrompt(context.Prompt, Math.Max(1, _options.MaxDraftPromptChars)));
        prompt.AppendLine();
        prompt.AppendLine("Notebook context:");
        prompt.AppendLine(BuildNotebookContext(context));
        return prompt.ToString();
    }

    private string BuildNotebookContext(AiNotebookEditGenerationContext context)
    {
        var budget = Math.Max(1, _options.MaxDraftContextChars);
        var builder = new StringBuilder();

        AppendLineWithinBudget(builder, budget, $"Notebook: {context.Notebook.Title} ({context.Notebook.Slug})");
        if (!string.IsNullOrWhiteSpace(context.Notebook.Description))
        {
            AppendLineWithinBudget(builder, budget, $"Description: {context.Notebook.Description}");
        }

        if (context.ActivePage is not null)
        {
            AppendLineWithinBudget(builder, budget, string.Empty);
            AppendLineWithinBudget(builder, budget, $"Active page: {context.ActivePage.Title} ({context.ActivePage.Path})");
            AppendLineWithinBudget(builder, budget, "Active page plain text:");
            AppendLineWithinBudget(
                builder,
                budget,
                TrimForPrompt(context.ActivePage.PlainTextContent ?? string.Empty, ActivePagePreviewChars));

            var contentJson = context.ActivePage.ContentJson?.GetRawText();
            if (!string.IsNullOrWhiteSpace(contentJson) && contentJson.Length <= ActivePageJsonChars)
            {
                AppendLineWithinBudget(builder, budget, "Active page TipTap JSON:");
                AppendLineWithinBudget(builder, budget, TrimForPrompt(contentJson, ActivePageJsonChars));
            }
            else if (context.ActivePage.ContentJson is JsonElement activePageContentJson)
            {
                AppendLineWithinBudget(builder, budget, "Active page top-level block outline:");
                AppendBlockOutline(builder, budget, activePageContentJson);
                AppendLineWithinBudget(builder, budget, "Use block index operations or replace_text when possible for large pages.");
            }
        }

        AppendLineWithinBudget(builder, budget, string.Empty);
        AppendLineWithinBudget(builder, budget, "Visible notebook items:");
        foreach (var item in context.Notebook.Items.OrderBy(item => item.Path))
        {
            var line = new StringBuilder();
            line.Append($"- {item.Type}: {item.Title} ({item.Path})");
            if (!string.IsNullOrWhiteSpace(item.TextPreview))
            {
                line.Append(" :: ");
                line.Append(TrimForPrompt(item.TextPreview, ItemPreviewChars));
            }

            if (!AppendLineWithinBudget(builder, budget, line.ToString()))
            {
                AppendLineWithinBudget(builder, budget, "[context truncated]");
                break;
            }
        }

        return builder.ToString();
    }

    private static string GetFallbackTitle(string operation, AiNotebookEditGenerationContext context)
    {
        return operation == "create_page"
            ? $"{context.Notebook.Title} AI note"
            : context.ActivePage?.Title ?? $"{context.Notebook.Title} AI edit";
    }

    private static string GetFallbackSummary(string operation, AiNotebookEditGenerationContext context)
    {
        return operation switch
        {
            "create_page" => $"Create a new page in notebook '{context.Notebook.Slug}'.",
            "append_to_current_page" => $"Append content to page '{context.ActivePage?.Path ?? "current-page"}'.",
            "delete_page" => $"Delete page '{context.ActivePage?.Path ?? "current-page"}'.",
            _ => $"Update page '{context.ActivePage?.Path ?? "current-page"}'."
        };
    }

    private static string StripCodeFence(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```json".Length..].TrimStart();
        }
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed["```".Length..].TrimStart();
        }

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^"```".Length].TrimEnd();
        }

        return trimmed;
    }

    private static bool AppendLineWithinBudget(StringBuilder builder, int budget, string value)
    {
        var remaining = budget - builder.Length;
        if (remaining <= 0)
        {
            return false;
        }

        if (value.Length + Environment.NewLine.Length <= remaining)
        {
            builder.AppendLine(value);
            return true;
        }

        if (remaining > "[truncated]".Length + Environment.NewLine.Length)
        {
            builder.Append(value.AsSpan(0, remaining - "[truncated]".Length - Environment.NewLine.Length));
            builder.AppendLine("[truncated]");
        }

        return false;
    }

    private static string TrimForPrompt(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxChars
            ? normalized
            : string.Concat(normalized.AsSpan(0, maxChars), "\n[truncated]");
    }

    private void AppendBlockOutline(StringBuilder builder, int budget, JsonElement contentJson)
    {
        if (!contentJson.TryGetProperty("content", out var blocks)
            || blocks.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var block in blocks.EnumerateArray())
        {
            var blockType = block.TryGetProperty("type", out var typeProperty) && typeProperty.ValueKind == JsonValueKind.String
                ? typeProperty.GetString()
                : "unknown";
            var snippet = TrimForPrompt(plainTextExtractor.Extract(block) ?? string.Empty, BlockSnippetChars);
            if (!AppendLineWithinBudget(builder, budget, $"- block[{index}] {blockType}: {snippet}"))
            {
                AppendLineWithinBudget(builder, budget, "[block outline truncated]");
                break;
            }

            index++;
        }
    }

    private const string EditInstructions = """
        You are CodeCafe Assistant generating notebook edit proposals.
        Return only one JSON object with this shape:
        {
          "action": "replace_current_page" | "append_to_current_page" | "create_page" | "delete_page",
          "mode": "full_document" | "operations",
          "title": "string",
          "summary": "short plain-language summary",
          "contentJson": { "type": "doc", "content": [] },
          "operations": []
        }

        Rules:
        - The notebook content format is TipTap JSON, not Markdown.
        - Use mode=full_document when you can safely provide the whole final TipTap document.
        - Use mode=operations when the page is large or when a targeted edit is safer than rewriting the entire page.
        - contentJson must be a full TipTap document rooted at {"type":"doc"}.
        - operations must be an array of supported operation objects.
        - Supported operation types are:
          - {"type":"append_blocks","blocks":[...]}
          - {"type":"replace_block_at_index","index":0,"block":{...}}
          - {"type":"insert_blocks_at_index","index":0,"blocks":[...]}
          - {"type":"delete_block_at_index","index":0}
          - {"type":"replace_text_in_block","index":0,"searchText":"old","replacementText":"new","replaceAll":false}
          - {"type":"replace_text","searchText":"old","replacementText":"new","replaceAll":false}
        - Prefer replace_block_at_index, insert_blocks_at_index, or replace_text_in_block when editing large pages from block outlines.
        - Use document-wide replace_text only when the exact target text is clearly visible and uniquely identifiable in the provided context.
        - For create_page, prefer mode=full_document and provide a good page title.
        - For delete_page, omit mode, contentJson, and operations. The active page will be archived.
        - Respect the requested operation when it is not "auto".
        - If the request is "auto", choose the best action based on the user's intent and notebook context.
        - Use only content that is supported by the provided notebook context or safe editorial transformation of it.
        - Do not include Markdown fences, explanations, or any text outside the JSON object.
        """;
}
