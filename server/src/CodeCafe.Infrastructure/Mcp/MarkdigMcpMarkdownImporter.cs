using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Mcp;
using CodeCafe.Application.Notes;
using Markdig;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.MediaLinks;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace CodeCafe.Infrastructure.Mcp;

public interface IMcpMarkdownImporter
{
    JsonElement ConvertMarkdownToDocument(string markdown);

    JsonElement ConvertMarkdownToBlocks(string markdown);
}

public sealed class MarkdigMcpMarkdownImporter : IMcpMarkdownImporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private static readonly MediaOptions MediaOptions = new();
    private static readonly HashSet<string> YouTubeHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "youtu.be",
        "youtube-nocookie.com",
        "www.youtube-nocookie.com"
    };
    private static readonly HashSet<string> OtherMediaProviderHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "vimeo.com",
        "music.yandex.ru",
        "ok.ru"
    };

    public JsonElement ConvertMarkdownToDocument(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        var content = EnsureNonEmptyBlocks(ConvertBlocks(document));
        var root = new JsonObject
        {
            ["type"] = "doc",
            ["content"] = content
        };

        return JsonSerializer.SerializeToElement(root, SerializerOptions);
    }

    public JsonElement ConvertMarkdownToBlocks(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        return JsonSerializer.SerializeToElement(ConvertBlocks(document), SerializerOptions);
    }

    private static JsonArray ConvertBlocks(ContainerBlock container)
    {
        var blocks = new JsonArray();
        foreach (var block in container)
        {
            foreach (var converted in ConvertBlock(block))
            {
                blocks.Add(converted);
            }
        }

        return blocks;
    }

    private static IEnumerable<JsonNode> ConvertBlock(Block block)
    {
        if (IsMarkdigInfrastructureBlock(block))
        {
            yield break;
        }

        switch (block)
        {
            case HeadingBlock heading:
                yield return new JsonObject
                {
                    ["type"] = "heading",
                    ["attrs"] = new JsonObject { ["level"] = Math.Clamp(heading.Level, 1, 6) },
                    ["content"] = ConvertInlineContainer(heading.Inline)
                };
                yield break;

            case ParagraphBlock paragraph:
                foreach (var converted in ConvertParagraph(paragraph))
                {
                    yield return converted;
                }
                yield break;

            case QuoteBlock quote:
                yield return new JsonObject
                {
                    ["type"] = "blockquote",
                    ["content"] = EnsureNonEmptyBlocks(ConvertBlocks(quote))
                };
                yield break;

            case MathBlock mathBlock:
                yield return CreateCodeBlock("latex", ExtractLeafBlockLines(mathBlock));
                yield break;

            case FencedCodeBlock fencedCode:
                yield return CreateCodeBlock(fencedCode.Info, ExtractLeafBlockLines(fencedCode));
                yield break;

            case CodeBlock codeBlock:
                yield return CreateCodeBlock(null, ExtractLeafBlockLines(codeBlock));
                yield break;

            case ThematicBreakBlock:
                yield return new JsonObject { ["type"] = "horizontalRule" };
                yield break;

            case ListBlock list:
                yield return ConvertList(list);
                yield break;

            case Table table:
                yield return ConvertTable(table);
                yield break;

            case HtmlBlock html:
                yield return CreateParagraphFromText(ExtractLeafBlockLines(html).TrimEnd('\r', '\n'));
                yield break;

            // Advanced Markdig extensions introduce additional leaf and container block
            // types. Flatten unsupported structures instead of calling ToString(), which
            // returns CLR type names for most AST nodes.
            case LeafBlock leaf when leaf.Inline is not null:
                foreach (var converted in ConvertInlineSequenceToBlocks(leaf.Inline))
                {
                    yield return converted;
                }
                yield break;

            case ContainerBlock nested:
                foreach (var childBlock in nested)
                {
                    foreach (var converted in ConvertBlock(childBlock))
                    {
                        yield return converted;
                    }
                }
                yield break;

            default:
                yield break;
        }
    }

    private static IEnumerable<JsonNode> ConvertParagraph(ParagraphBlock paragraph)
        => ConvertInlineSequenceToBlocks(
            paragraph.Inline ?? Enumerable.Empty<Inline>(),
            emitEmptyParagraph: paragraph.Inline?.FirstChild is null);

    private static IEnumerable<JsonNode> ConvertInlineSequenceToBlocks(
        IEnumerable<Inline> inlines,
        bool trimLeadingText = false,
        bool emitEmptyParagraph = false)
    {
        var inlineContent = new JsonArray();
        var processedFirstNode = false;
        foreach (var inline in inlines)
        {
            foreach (var node in ConvertInline(inline, activeMarks: null))
            {
                var converted = trimLeadingText && !processedFirstNode
                    ? TrimLeadingTextNode(node)
                    : node;
                if (converted is null)
                {
                    continue;
                }

                processedFirstNode = true;
                if (IsBlockNode(converted))
                {
                    if (inlineContent.Count > 0)
                    {
                        yield return CreateParagraph(inlineContent);
                        inlineContent = new JsonArray();
                    }

                    yield return converted;
                    continue;
                }

                inlineContent.Add(converted);
            }
        }

        if (inlineContent.Count > 0 || emitEmptyParagraph)
        {
            yield return CreateParagraph(inlineContent);
        }
    }

    private static JsonObject CreateParagraph(JsonArray content)
        => new()
        {
            ["type"] = "paragraph",
            ["content"] = content
        };

    private static JsonArray EnsureNonEmptyBlocks(JsonArray content)
    {
        if (content.Count == 0)
        {
            content.Add(CreateParagraph(new JsonArray()));
        }

        return content;
    }

    private static bool IsBlockNode(JsonNode node)
        => node is JsonObject obj
           && TryGetString(obj, "type", out var type)
           && type is "image" or "youtube";

    private static JsonObject ConvertList(ListBlock list)
    {
        var listItems = list.OfType<ListItemBlock>().ToList();
        if (!list.IsOrdered && listItems.Count > 0 && listItems.All(IsTaskListItem))
        {
            return ConvertTaskList(listItems);
        }

        var listType = list.IsOrdered ? "orderedList" : "bulletList";
        var items = new JsonArray();
        foreach (var child in listItems)
        {
            items.Add(new JsonObject
            {
                ["type"] = "listItem",
                ["content"] = EnsureLeadingParagraph(ConvertBlocks(child))
            });
        }

        var node = new JsonObject
        {
            ["type"] = listType,
            ["content"] = items
        };

        if (list.IsOrdered)
        {
            var attrs = new JsonObject();
            if (int.TryParse(list.OrderedStart, out var orderedStart) && orderedStart != 1)
            {
                attrs["start"] = orderedStart;
            }

            if (list.BulletType is 'a' or 'A' or 'i' or 'I')
            {
                attrs["type"] = list.BulletType.ToString();
            }

            if (attrs.Count > 0)
            {
                node["attrs"] = attrs;
            }
        }

        return node;
    }

    private static JsonObject ConvertTaskList(IEnumerable<ListItemBlock> listItems)
    {
        var items = new JsonArray();
        foreach (var child in listItems)
        {
            var isTaskItem = TryGetTaskListItem(child, out var isChecked);
            var content = isTaskItem
                ? ConvertTaskItemBlocks(child)
                : ConvertBlocks(child);
            items.Add(new JsonObject
            {
                ["type"] = "taskItem",
                ["attrs"] = new JsonObject { ["checked"] = isTaskItem && isChecked },
                ["content"] = EnsureLeadingParagraph(content)
            });
        }

        return new JsonObject
        {
            ["type"] = "taskList",
            ["content"] = items
        };
    }

    private static JsonArray EnsureLeadingParagraph(JsonArray content)
    {
        if (content.Count == 0
            || content[0] is not JsonObject firstBlock
            || !TryGetString(firstBlock, "type", out var firstType)
            || !string.Equals(firstType, "paragraph", StringComparison.Ordinal))
        {
            content.Insert(0, CreateParagraph(new JsonArray()));
        }

        return content;
    }

    private static bool IsTaskListItem(ListItemBlock item)
        => TryGetTaskListItem(item, out _);

    private static bool TryGetTaskListItem(ListItemBlock item, out bool isChecked)
    {
        isChecked = false;
        if (item.FirstOrDefault() is not ParagraphBlock paragraph
            || paragraph.Inline?.FirstChild is not TaskList taskList)
        {
            return false;
        }

        isChecked = taskList.Checked;
        return true;
    }

    private static JsonArray ConvertTaskItemBlocks(ListItemBlock item)
    {
        var blocks = new JsonArray();
        var removedTaskMarker = false;
        foreach (var block in item)
        {
            if (!removedTaskMarker && block is ParagraphBlock paragraph && IsTaskParagraph(paragraph))
            {
                var remainingInlines = paragraph.Inline?.Skip(1) ?? Enumerable.Empty<Inline>();
                foreach (var converted in ConvertInlineSequenceToBlocks(
                             remainingInlines,
                             trimLeadingText: true))
                {
                    blocks.Add(converted);
                }

                removedTaskMarker = true;
                continue;
            }

            foreach (var converted in ConvertBlock(block))
            {
                blocks.Add(converted);
            }
        }

        if (blocks.Count == 0)
        {
            blocks.Add(new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray()
            });
        }

        return blocks;
    }

    private static bool IsTaskParagraph(ParagraphBlock paragraph)
        => paragraph.Inline?.FirstChild is TaskList;

    private static JsonObject ConvertTable(Table table)
    {
        var rows = new JsonArray();
        foreach (var rowObj in table.OfType<TableRow>())
        {
            var cells = new JsonArray();
            var logicalColumnIndex = 0;
            foreach (var cell in rowObj.OfType<TableCell>())
            {
                var cellNode = new JsonObject
                {
                    ["type"] = rowObj.IsHeader ? "tableHeader" : "tableCell",
                    ["content"] = NormalizeTableCellContent(ConvertBlocks(cell))
                };
                var attrs = new JsonObject();
                if (cell.ColumnSpan > 1)
                {
                    attrs["colspan"] = cell.ColumnSpan;
                }

                if (cell.RowSpan > 1)
                {
                    attrs["rowspan"] = cell.RowSpan;
                }

                var columnIndex = cell.ColumnIndex >= 0 ? cell.ColumnIndex : logicalColumnIndex;
                if (columnIndex < table.ColumnDefinitions.Count
                    && ConvertTableAlignment(table.ColumnDefinitions[columnIndex].Alignment) is { } alignment)
                {
                    attrs["align"] = alignment;
                }

                if (attrs.Count > 0)
                {
                    cellNode["attrs"] = attrs;
                }

                cells.Add(cellNode);
                logicalColumnIndex = columnIndex + Math.Max(cell.ColumnSpan, 1);
            }

            rows.Add(new JsonObject
            {
                ["type"] = "tableRow",
                ["content"] = cells
            });
        }

        return new JsonObject
        {
            ["type"] = "table",
            ["content"] = rows
        };
    }

    private static string? ConvertTableAlignment(TableColumnAlign? alignment)
        => alignment switch
        {
            TableColumnAlign.Left => "left",
            TableColumnAlign.Center => "center",
            TableColumnAlign.Right => "right",
            _ => null
        };

    private static JsonArray NormalizeTableCellContent(JsonArray content)
    {
        if (content.Count > 0)
        {
            return content;
        }

        return new JsonArray
        {
            new JsonObject
            {
                ["type"] = "paragraph",
                ["content"] = new JsonArray()
            }
        };
    }

    private static JsonObject CreateCodeBlock(string? info, string text)
    {
        var content = new JsonArray();
        if (!string.IsNullOrEmpty(text))
        {
            content.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        var node = new JsonObject
        {
            ["type"] = "codeBlock",
            ["content"] = content
        };

        var language = NormalizeInfoString(info);
        if (!string.IsNullOrWhiteSpace(language))
        {
            node["attrs"] = new JsonObject { ["language"] = language };
        }

        return node;
    }

    private static string? NormalizeInfoString(string? info)
    {
        if (string.IsNullOrWhiteSpace(info))
        {
            return null;
        }

        var trimmed = info.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        return firstSpace >= 0 ? trimmed[..firstSpace] : trimmed;
    }

    private static JsonObject CreateParagraphFromText(string text)
    {
        var content = new JsonArray();
        if (!string.IsNullOrEmpty(text))
        {
            content.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = text
            });
        }

        return new JsonObject
        {
            ["type"] = "paragraph",
            ["content"] = content
        };
    }

    private static JsonArray ConvertInlineContainer(ContainerInline? container, IReadOnlyList<JsonObject>? marks = null)
    {
        var nodes = new JsonArray();
        foreach (var node in ConvertInlineChildren(container, marks))
        {
            if (IsBlockNode(node))
            {
                if (node is JsonObject blockNode
                    && blockNode["attrs"] is JsonObject attrs)
                {
                    if (TryGetString(attrs, "alt", out var alt) && !string.IsNullOrEmpty(alt))
                    {
                        nodes.Add(CreateTextNode(alt, marks));
                    }
                    else if (TryGetString(attrs, "src", out var src) && !string.IsNullOrEmpty(src))
                    {
                        nodes.Add(CreateTextNode(src, marks));
                    }
                }

                continue;
            }

            nodes.Add(node);
        }

        return nodes;
    }

    private static IEnumerable<JsonNode> ConvertInlineChildren(ContainerInline? container, IReadOnlyList<JsonObject>? marks = null)
    {
        if (container is null)
        {
            yield break;
        }

        foreach (var inline in container)
        {
            foreach (var node in ConvertInline(inline, marks))
            {
                yield return node;
            }
        }
    }

    private static IEnumerable<JsonNode> ConvertInline(Inline inline, IReadOnlyList<JsonObject>? activeMarks)
    {
        switch (inline)
        {
            case LiteralInline literal:
                var text = literal.Content.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    yield return CreateTextNode(text, activeMarks);
                }
                yield break;

            case LineBreakInline breakInline:
                if (breakInline.IsHard)
                {
                    yield return new JsonObject { ["type"] = "hardBreak" };
                    yield break;
                }

                yield return CreateTextNode(" ", activeMarks);
                yield break;

            case CodeInline code:
                if (string.IsNullOrEmpty(code.Content))
                {
                    yield break;
                }

                yield return CreateTextNode(code.Content, AddMark(activeMarks, new JsonObject { ["type"] = "code" }));
                yield break;

            case MathInline math:
                var mathContent = math.Content.ToString();
                if (!string.IsNullOrEmpty(mathContent))
                {
                    yield return CreateTextNode(
                        $"{new string(math.Delimiter, math.DelimiterCount)}{mathContent}{new string(math.Delimiter, math.DelimiterCount)}",
                        AddMark(activeMarks, new JsonObject { ["type"] = "code" }));
                }
                yield break;

            case HtmlEntityInline entity:
                var transcoded = entity.Transcoded.ToString();
                if (!string.IsNullOrEmpty(transcoded))
                {
                    yield return CreateTextNode(transcoded, activeMarks);
                }
                yield break;

            case AutolinkInline autolink:
                var href = autolink.IsEmail && !autolink.Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    ? $"mailto:{autolink.Url}"
                    : autolink.Url;
                var autolinkMarks = ContentUrlPolicy.IsAllowedLinkUrl(href)
                    ? AddMark(activeMarks, new JsonObject
                    {
                        ["type"] = "link",
                        ["attrs"] = new JsonObject { ["href"] = href }
                    })
                    : activeMarks;
                yield return CreateTextNode(autolink.Url, autolinkMarks);
                yield break;

            case AbbreviationInline abbreviation:
                if (!string.IsNullOrEmpty(abbreviation.Abbreviation.Label))
                {
                    yield return CreateTextNode(abbreviation.Abbreviation.Label, activeMarks);
                }
                yield break;

            case FootnoteLink footnoteLink:
                if (!footnoteLink.IsBackLink)
                {
                    yield return CreateTextNode($"[{footnoteLink.Footnote.Order}]", activeMarks);
                }
                yield break;

            case TaskList taskList:
                yield return CreateTextNode(taskList.Checked ? "[x]" : "[ ]", activeMarks);
                yield break;

            case PipeTableDelimiterInline pipeDelimiter:
                yield return CreateTextNode("|", activeMarks);
                foreach (var child in ConvertInlineChildren(pipeDelimiter, activeMarks))
                {
                    yield return child;
                }
                yield break;

            case LinkInline link when link.IsImage:
                if (TryGetYouTubeUrl(link.Url, out var youtubeUrl))
                {
                    yield return new JsonObject
                    {
                        ["type"] = "youtube",
                        ["attrs"] = new JsonObject { ["src"] = youtubeUrl }
                    };
                    yield break;
                }

                if (IsUnsupportedMediaLink(link.Url))
                {
                    var label = ExtractInlineText(link);
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        label = link.Url ?? string.Empty;
                    }

                    if (!string.IsNullOrEmpty(label))
                    {
                        var mediaMarks = ContentUrlPolicy.IsAllowedLinkUrl(link.Url)
                            ? AddMark(activeMarks, CreateLinkMark(link.Url!))
                            : activeMarks;
                        yield return CreateTextNode(label, mediaMarks);
                    }
                    yield break;
                }

                var imageAttributes = new JsonObject();
                if (ContentUrlPolicy.IsAllowedResourceUrl(link.Url))
                {
                    imageAttributes["src"] = link.Url ?? string.Empty;
                }

                imageAttributes["alt"] = ExtractInlineText(link);
                imageAttributes["title"] = string.IsNullOrWhiteSpace(link.Title) ? null : link.Title;
                yield return new JsonObject
                {
                    ["type"] = "image",
                    ["attrs"] = imageAttributes
                };
                yield break;

            case LinkInline link:
                // Links with unsafe URLs are imported as plain text without the link mark.
                var linkMarks = ContentUrlPolicy.IsAllowedLinkUrl(link.Url)
                    ? AddMark(activeMarks, CreateLinkMark(link.Url ?? string.Empty))
                    : activeMarks;

                if (link.FirstChild is null && !string.IsNullOrWhiteSpace(link.Url))
                {
                    yield return CreateTextNode(link.Url!, linkMarks);
                    yield break;
                }

                foreach (var child in ConvertInlineChildren(link, linkMarks))
                {
                    yield return child;
                }
                yield break;

            case EmphasisInline emphasis:
                var emphasisMarks = AddEmphasisMarks(activeMarks, emphasis);
                foreach (var child in ConvertInlineChildren(emphasis, emphasisMarks))
                {
                    yield return child;
                }
                yield break;

            case HtmlInline htmlInline:
                if (!string.IsNullOrWhiteSpace(htmlInline.Tag))
                {
                    yield return CreateTextNode(htmlInline.Tag, activeMarks);
                }
                yield break;

            // Preserve visible descendants of extension-defined inline containers. Never
            // serialize an AST object's ToString() value: most Markdig nodes return their
            // CLR type name rather than user-authored text.
            case ContainerInline nested:
                foreach (var child in ConvertInlineChildren(nested, activeMarks))
                {
                    yield return child;
                }
                yield break;

            default:
                yield break;
        }
    }

    private static JsonObject CreateLinkMark(string href)
        => new()
        {
            ["type"] = "link",
            ["attrs"] = new JsonObject { ["href"] = href }
        };

    private static bool TryGetYouTubeUrl(string? value, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !YouTubeHosts.Contains(uri.Host))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? videoId;
        if (string.Equals(uri.Host, "youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            videoId = segments.Length == 1 ? segments[0] : null;
        }
        else if (segments.Length == 2 && segments[0] is "embed" or "shorts")
        {
            videoId = segments[1];
        }
        else if (segments.Length == 1 && string.Equals(segments[0], "watch", StringComparison.OrdinalIgnoreCase))
        {
            videoId = GetQueryParameter(uri.Query, "v");
        }
        else
        {
            videoId = null;
        }

        if (videoId is null
            || videoId.Length < 6
            || videoId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            return false;
        }

        url = $"https://www.youtube.com/watch?v={videoId}";
        return true;
    }

    private static string? GetQueryParameter(string query, string name)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator >= 0 ? pair[..separator] : pair;
            if (!string.Equals(Uri.UnescapeDataString(key.Replace('+', ' ')), name, StringComparison.Ordinal))
            {
                continue;
            }

            var value = separator >= 0 ? pair[(separator + 1)..] : string.Empty;
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return null;
    }

    private static bool IsUnsupportedMediaLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
        {
            return false;
        }

        if (uri.IsAbsoluteUri && OtherMediaProviderHosts.Contains(uri.Host))
        {
            foreach (var provider in MediaOptions.Hosts)
            {
                if (provider.TryHandle(uri, isSchemaRelative: false, out _))
                {
                    return true;
                }
            }
        }

        var path = uri.IsAbsoluteUri
            ? uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)
            : uri.OriginalString.Split('?', '#')[0];
        return MediaOptions.ExtensionToMimeType.ContainsKey(Path.GetExtension(path));
    }

    private static JsonNode? TrimLeadingTextNode(JsonNode node)
    {
        if (node is not JsonObject obj
            || !TryGetString(obj, "type", out var type)
            || !string.Equals(type, "text", StringComparison.Ordinal)
            || !TryGetString(obj, "text", out var text))
        {
            return node;
        }

        var trimmed = text.TrimStart();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (string.Equals(trimmed, text, StringComparison.Ordinal))
        {
            return node;
        }

        var clone = obj.DeepClone().AsObject();
        clone["text"] = trimmed;
        return clone;
    }

    private static bool TryGetString(JsonObject obj, string propertyName, out string value)
    {
        value = string.Empty;
        if (obj[propertyName] is not JsonValue jsonValue
            || !jsonValue.TryGetValue<string?>(out var candidate)
            || candidate is null)
        {
            return false;
        }

        value = candidate;
        return true;
    }

    private static JsonObject CreateTextNode(string text, IReadOnlyList<JsonObject>? marks)
    {
        var node = new JsonObject
        {
            ["type"] = "text",
            ["text"] = text
        };

        if (marks is { Count: > 0 })
        {
            var effectiveMarks = HasMark(marks, "code")
                ? marks.Where(mark => IsMarkType(mark, "code"))
                : marks;
            var marksArray = new JsonArray();
            foreach (var mark in effectiveMarks.DistinctBy(GetMarkType))
            {
                marksArray.Add(mark.DeepClone());
            }

            if (marksArray.Count > 0)
            {
                node["marks"] = marksArray;
            }
        }

        return node;
    }

    private static bool HasMark(IEnumerable<JsonObject> marks, string type)
        => marks.Any(mark => IsMarkType(mark, type));

    private static bool IsMarkType(JsonObject mark, string type)
        => TryGetString(mark, "type", out var markType)
           && string.Equals(markType, type, StringComparison.Ordinal);

    private static string GetMarkType(JsonObject mark)
        => TryGetString(mark, "type", out var type) ? type : string.Empty;

    private static IReadOnlyList<JsonObject> AddEmphasisMarks(IReadOnlyList<JsonObject>? marks, EmphasisInline emphasis)
    {
        var result = marks?.ToList() ?? [];
        var markType = emphasis.DelimiterChar switch
        {
            '~' when emphasis.DelimiterCount >= 2 => "strike",
            '~' => "subscript",
            '^' => "superscript",
            '+' when emphasis.DelimiterCount >= 2 => "underline",
            '=' when emphasis.DelimiterCount >= 2 => "highlight",
            '"' => "italic",
            '*' or '_' when emphasis.DelimiterCount >= 2 => "bold",
            '*' or '_' => "italic",
            _ => null
        };

        if (markType is not null)
        {
            result.Add(new JsonObject { ["type"] = markType });
        }

        return result;
    }

    private static IReadOnlyList<JsonObject> AddMark(IReadOnlyList<JsonObject>? marks, JsonObject mark)
    {
        var result = marks?.ToList() ?? [];
        result.Add(mark);
        return result;
    }

    private static string ExtractInlineText(ContainerInline inline)
    {
        var builder = new StringBuilder();
        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case HtmlEntityInline entity:
                    builder.Append(entity.Transcoded.ToString());
                    break;
                case AbbreviationInline abbreviation:
                    builder.Append(abbreviation.Abbreviation.Label);
                    break;
                case MathInline math:
                    builder.Append(math.Content.ToString());
                    break;
                case ContainerInline nested:
                    builder.Append(ExtractInlineText(nested));
                    break;
            }
        }

        return builder.ToString();
    }

    private static string ExtractLeafBlockLines(LeafBlock block)
        => block.Lines.Count > 0 ? block.Lines.ToString() : string.Empty;

    private static bool IsMarkdigInfrastructureBlock(Block block)
        => block is LinkReferenceDefinitionGroup;
}
