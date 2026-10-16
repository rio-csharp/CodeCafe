using System.Text.Json;

namespace CodeCafe.Application.Common.Markdown;

// One block of parsed markdown: the wire type name, the canonical payload JSON (already
// normalized, ready to store), its search projection, and nested children (list nesting).
internal sealed record ParsedMarkdownBlock(
    string Type,
    JsonElement Content,
    string PlainText,
    IReadOnlyList<ParsedMarkdownBlock> Children);
