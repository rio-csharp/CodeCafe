using CodeCafe.Application.Ai.Drafts;
using CodeCafe.Application.Ai.Edits;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.Options;
using OpenAI;
using System.Text;

namespace CodeCafe.Infrastructure.Ai;

public sealed class OpenAiNoteDraftGenerator(
    OpenAIClient openAiClient,
    IOptions<AiOptions> aiOptionsAccessor) : IAiNoteDraftGenerator
{
    private const int ActivePagePreviewChars = 2400;
    private const int ItemPreviewChars = 900;

    private readonly AiOptions _options = aiOptionsAccessor.Value;

    public async Task<AiNoteDraftResult> GenerateDraftAsync(
        AiNoteDraftGenerationContext context,
        CancellationToken cancellationToken)
    {
        var markdown = await OpenAiTextCompletion.CompleteAsync(
            openAiClient,
            _options,
            DraftInstructions,
            BuildUserPrompt(context),
            context.CurrentUserId.ToString("N"),
            cancellationToken);

        return new AiNoteDraftResult(CleanMarkdown(markdown));
    }

    private string BuildUserPrompt(AiNoteDraftGenerationContext context)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Generate a Markdown draft for CodeCafe notes.");
        prompt.AppendLine($"Locale: {context.Locale}");
        prompt.AppendLine($"Intent: {context.Intent}");
        prompt.AppendLine();
        prompt.AppendLine("User request:");
        prompt.AppendLine(AiHelpers.TrimForPrompt(context.Prompt, Math.Max(1, _options.MaxDraftPromptChars)));
        prompt.AppendLine();
        prompt.AppendLine("Notebook context:");
        prompt.AppendLine(BuildNotebookContext(context));
        return prompt.ToString();
    }

    private string BuildNotebookContext(AiNoteDraftGenerationContext context)
    {
        var budget = Math.Max(1, _options.MaxDraftContextChars);
        var builder = new StringBuilder();

        AiHelpers.AppendLineWithinBudget(builder, budget, $"Notebook: {context.Notebook.Title} ({context.Notebook.Slug})");
        if (!string.IsNullOrWhiteSpace(context.Notebook.Description))
        {
            AiHelpers.AppendLineWithinBudget(builder, budget, $"Description: {context.Notebook.Description}");
        }

        if (context.ActivePage is not null)
        {
            AiHelpers.AppendLineWithinBudget(builder, budget, "");
            AiHelpers.AppendLineWithinBudget(builder, budget, $"Active page: {context.ActivePage.Title} ({context.ActivePage.Path})");
            AiHelpers.AppendLineWithinBudget(
                builder,
                budget,
                AiHelpers.TrimForPrompt(context.ActivePage.PlainTextContent ?? string.Empty, ActivePagePreviewChars));
        }

        AiHelpers.AppendLineWithinBudget(builder, budget, "");
        AiHelpers.AppendLineWithinBudget(builder, budget, "Visible notebook items:");
        foreach (var item in context.Notebook.Items.OrderBy(item => item.Path))
        {
            var itemText = new StringBuilder();
            itemText.Append($"- {item.Type}: {item.Title} ({item.Path})");
            if (!string.IsNullOrWhiteSpace(item.TextPreview))
            {
                itemText.AppendLine();
                itemText.Append(AiHelpers.TrimForPrompt(item.TextPreview, ItemPreviewChars));
            }

            if (!AiHelpers.AppendLineWithinBudget(builder, budget, itemText.ToString()))
            {
                AiHelpers.AppendLineWithinBudget(builder, budget, "[context truncated]");
                break;
            }
        }

        return builder.ToString();
    }

    private static string CleanMarkdown(string markdown)
    {
        var trimmed = markdown.Trim();
        if (trimmed.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```markdown".Length..].TrimStart();
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

    private const string DraftInstructions = """
        You are CodeCafe Assistant drafting notebook content.
        Return only Markdown that can be saved into a note; do not wrap the answer in a Markdown code fence.
        Use the provided notebook context as source material and avoid inventing facts that are not supported by it.
        Preserve code blocks when they are useful. Prefer clear headings, concise paragraphs, and actionable lists.
        When you rely on existing notes, cite notebook slugs and page paths in a short Sources section.
        Never claim that you changed the notebook; you are only producing a draft for the user to review.
        """;
}
