using CodeCafe.Modules.Notes.Application.Notes;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

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

    public JsonElement ConvertMarkdownToDocument(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        var content = ConvertBlocks(document);
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
                    ["attrs"] = new JsonObject { ["level"] = Math.Clamp(heading.Level, 1, 4) },
                    ["content"] = ConvertInlineContainer(heading.Inline)
                };
                yield break;

            case ParagraphBlock paragraph:
                yield return new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = ConvertInlineContainer(paragraph.Inline)
                };
                yield break;

            case QuoteBlock quote:
                yield return new JsonObject
                {
                    ["type"] = "blockquote",
                    ["content"] = ConvertBlocks(quote)
                };
                yield break;

            case FencedCodeBlock fencedCode:
                yield return CreateCodeBlock(fencedCode.Info, ExtractBlockText(fencedCode));
                yield break;

            case CodeBlock codeBlock:
                yield return CreateCodeBlock(null, ExtractBlockText(codeBlock));
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
                yield return CreateParagraphFromText(ExtractBlockText(html));
                yield break;

            default:
                var text = ExtractBlockText(block);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return CreateParagraphFromText(text);
                }
                yield break;
        }
    }

    private static JsonObject ConvertList(ListBlock list)
    {
        var listItems = list.OfType<ListItemBlock>().ToList();
        if (!list.IsOrdered && listItems.Any(IsTaskListItem))
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
                ["content"] = ConvertBlocks(child)
            });
        }

        var node = new JsonObject
        {
            ["type"] = listType,
            ["content"] = items
        };

        return node;
    }

    private static JsonObject ConvertTaskList(IEnumerable<ListItemBlock> listItems)
    {
        var items = new JsonArray();
        foreach (var child in listItems)
        {
            var isTaskItem = TryGetTaskListItem(child, out var isChecked);
            items.Add(new JsonObject
            {
                ["type"] = "taskItem",
                ["attrs"] = new JsonObject { ["checked"] = isTaskItem && isChecked },
                ["content"] = isTaskItem
                    ? ConvertTaskItemBlocks(child)
                    : ConvertBlocks(child)
            });
        }

        return new JsonObject
        {
            ["type"] = "taskList",
            ["content"] = items
        };
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
                blocks.Add(new JsonObject
                {
                    ["type"] = "paragraph",
                    ["content"] = ConvertTaskParagraphInlineContainer(paragraph.Inline)
                });
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
            foreach (var cell in rowObj.OfType<TableCell>())
            {
                cells.Add(new JsonObject
                {
                    ["type"] = rowObj.IsHeader ? "tableHeader" : "tableCell",
                    ["content"] = NormalizeTableCellContent(ConvertBlocks(cell))
                });
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

    private static JsonArray ConvertTaskParagraphInlineContainer(ContainerInline? container)
    {
        var nodes = new JsonArray();
        if (container is null)
        {
            return nodes;
        }

        var skippedMarker = false;
        var trimmedFirstText = false;
        foreach (var inline in container)
        {
            if (!skippedMarker && inline is TaskList)
            {
                skippedMarker = true;
                continue;
            }

            foreach (var node in ConvertInline(inline, activeMarks: null))
            {
                var convertedNode = trimmedFirstText ? node : TrimLeadingTextNode(node);
                if (convertedNode is null)
                {
                    continue;
                }

                trimmedFirstText = true;
                nodes.Add(convertedNode);
            }
        }

        return nodes;
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

            case TaskList taskList:
                yield return CreateTextNode(taskList.Checked ? "[x]" : "[ ]", activeMarks);
                yield break;

            case LinkInline link when link.IsImage:
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
                    ? AddMark(activeMarks, new JsonObject
                    {
                        ["type"] = "link",
                        ["attrs"] = new JsonObject { ["href"] = link.Url ?? string.Empty }
                    })
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

            default:
                var fallback = inline.ToString();
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    yield return CreateTextNode(fallback, activeMarks);
                }
                yield break;
        }
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
            var marksArray = new JsonArray();
            foreach (var mark in marks)
            {
                marksArray.Add(mark.DeepClone());
            }

            node["marks"] = marksArray;
        }

        return node;
    }

    private static IReadOnlyList<JsonObject> AddEmphasisMarks(IReadOnlyList<JsonObject>? marks, EmphasisInline emphasis)
    {
        var result = marks?.ToList() ?? [];

        if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount >= 2)
        {
            result.Add(new JsonObject { ["type"] = "strike" });
            return result;
        }

        if (emphasis.DelimiterCount >= 2)
        {
            result.Add(new JsonObject { ["type"] = "bold" });
        }
        else
        {
            result.Add(new JsonObject { ["type"] = "italic" });
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
                case ContainerInline nested:
                    builder.Append(ExtractInlineText(nested));
                    break;
            }
        }

        return builder.ToString();
    }

    private static string ExtractBlockText(Block block)
    {
        return block switch
        {
            LeafBlock leaf when leaf.Lines.Count > 0 => leaf.Lines.ToString().TrimEnd(),
            _ => block.ToString() ?? string.Empty
        };
    }

    private static bool IsMarkdigInfrastructureBlock(Block block)
        => string.Equals(
            block.GetType().FullName,
            "Markdig.Syntax.LinkReferenceDefinitionGroup",
            StringComparison.Ordinal);
}
