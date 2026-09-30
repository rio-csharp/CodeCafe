using System.Text;
using System.Text.Json;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Notebooks.ExportNotebook;

internal static class MarkdownExporter
{
    public static string Render(
        Notebook notebook,
        IReadOnlyList<Page> pages,
        IReadOnlyDictionary<Guid, IReadOnlyList<Block>> blocksByPage
    )
    {
        var orderedPages = OrderPages(notebook, pages);
        var output = new StringBuilder().Append("# ").AppendLine(EscapeText(notebook.Title)).AppendLine();

        foreach (var page in orderedPages)
        {
            output.Append("## ").AppendLine(EscapeText(page.Title)).AppendLine();
            if (blocksByPage.TryGetValue(page.Id, out var blocks))
            {
                RenderBlocks(output, page, blocks);
            }

            output.AppendLine();
        }

        return output.ToString().TrimEnd() + Environment.NewLine;
    }

    private static IReadOnlyList<Page> OrderPages(Notebook notebook, IReadOnlyList<Page> pages)
    {
        var byId = pages.ToDictionary(page => page.Id);
        var ordered = new List<Page>(pages.Count);
        var pending = new Stack<Page>();
        PushPageChain(pending, byId, notebook.FirstPageId);

        while (pending.Count > 0)
        {
            var page = pending.Pop();
            ordered.Add(page);
            PushPageChain(pending, byId, page.FirstChildId);
        }

        return ordered;
    }

    private static void PushPageChain(Stack<Page> pending, IReadOnlyDictionary<Guid, Page> byId, Guid? head)
    {
        var chain = new List<Page>();
        var visited = new HashSet<Guid>();
        for (var id = head; id is not null && visited.Add(id.Value) && byId.TryGetValue(id.Value, out var page); id = page.NextSiblingId)
        {
            chain.Add(page);
        }

        for (var index = chain.Count - 1; index >= 0; index--)
        {
            pending.Push(chain[index]);
        }
    }

    private static void RenderBlocks(StringBuilder output, Page page, IReadOnlyList<Block> blocks)
    {
        var pending = new Stack<(Block Block, int Depth)>();
        PushBlocks(pending, page, blocks, null, 0);

        while (pending.Count > 0)
        {
            var (block, depth) = pending.Pop();
            RenderBlock(output, block, depth);
            PushBlocks(pending, page, blocks, block.Id, depth + 1);
        }
    }

    private static void PushBlocks(Stack<(Block Block, int Depth)> pending, Page page, IReadOnlyList<Block> blocks, Guid? parentId, int depth)
    {
        var siblings = BlockChain.OrderByChain(page, blocks, parentId);
        for (var index = siblings.Count - 1; index >= 0; index--)
        {
            pending.Push((siblings[index], depth));
        }
    }

    private static void RenderBlock(StringBuilder output, Block block, int depth)
    {
        var indent = new string(' ', depth * 2);
        switch (block.Type)
        {
            case BlockTypes.Paragraph:
                output.Append(indent).AppendLine(RenderSpans(GetPayload<ParagraphPayload>(block).Spans));
                break;
            case BlockTypes.Heading:
            {
                var payload = GetPayload<HeadingPayload>(block);
                output.Append(indent).Append(new string('#', Math.Min(payload.Level + 2, 6))).Append(' ')
                    .AppendLine(RenderSpans(payload.Spans));
                break;
            }
            case BlockTypes.Todo:
            {
                var payload = GetPayload<TodoPayload>(block);
                output.Append(indent).Append(payload.Checked ? "- [x] " : "- [ ] ")
                    .AppendLine(RenderSpans(payload.Spans));
                break;
            }
            case BlockTypes.Code:
            {
                var payload = GetPayload<CodePayload>(block);
                output.Append(indent).Append("```").AppendLine(payload.Language);
                output.AppendLine(payload.Code);
                output.Append(indent).AppendLine("```");
                break;
            }
            case BlockTypes.Quote:
                RenderQuote(output, indent, RenderSpans(GetPayload<QuotePayload>(block).Spans));
                break;
            case BlockTypes.Callout:
            {
                var payload = GetPayload<CalloutPayload>(block);
                RenderQuote(output, indent, $"[!{payload.Variant.ToString().ToUpperInvariant()}]\n{RenderSpans(payload.Spans)}");
                break;
            }
            case BlockTypes.Divider:
                output.Append(indent).AppendLine("---");
                break;
            case BlockTypes.Image:
            {
                var payload = GetPayload<ImagePayload>(block);
                output.Append(indent).Append("![").Append(EscapeText(payload.Alt ?? string.Empty)).Append("](")
                    .Append(EscapeUrl(payload.Url)).AppendLine(")");
                if (!string.IsNullOrWhiteSpace(payload.Caption))
                {
                    output.Append(indent).Append("_").Append(EscapeText(payload.Caption)).AppendLine("_");
                }
                break;
            }
            case BlockTypes.Audio:
            {
                var payload = GetPayload<AudioPayload>(block);
                output.Append(indent).Append("[audio](").Append(EscapeUrl(payload.Url)).AppendLine(")");
                break;
            }
            default:
                output.Append(indent).Append("<!-- unsupported block: ").Append(EscapeText(block.Type)).AppendLine(" -->");
                break;
        }

        output.AppendLine();
    }

    private static void RenderQuote(StringBuilder output, string indent, string text)
    {
        foreach (var line in text.Split('\n'))
        {
            output.Append(indent).Append("> ").AppendLine(line);
        }
    }

    private static string RenderSpans(Spans spans)
        => string.Concat(spans.Items.Select(RenderSpan));

    private static string RenderSpan(Span span)
    {
        var text = EscapeText(span.Text);
        foreach (var mark in span.Marks)
        {
            text = mark switch
            {
                BoldMark => $"**{text}**",
                ItalicMark => $"*{text}*",
                UnderlineMark => $"<u>{text}</u>",
                StrikeMark => $"~~{text}~~",
                CodeMark => $"`{text}`",
                LinkMark link => $"[{text}]({EscapeUrl(link.Href)})",
                KbdMark => $"<kbd>{text}</kbd>",
                SupMark => $"<sup>{text}</sup>",
                SubMark => $"<sub>{text}</sub>",
                AbbrMark abbr => $"<abbr title=\"{EscapeAttribute(abbr.Title)}\">{text}</abbr>",
                ColorMark or HighlightMark => text,
                _ => text,
            };
        }

        return text;
    }

    private static TPayload GetPayload<TPayload>(Block block)
        => BlockPayloads.DeserializeForRead<TPayload>(block.ContentJson);

    private static string EscapeText(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);

    private static string EscapeUrl(string value) => value.Replace(")", "%29", StringComparison.Ordinal);

    private static string EscapeAttribute(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
