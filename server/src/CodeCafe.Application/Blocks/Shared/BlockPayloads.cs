using System.Text.Json;

using CodeCafe.Application.Blocks.Shared.Payloads;
using CodeCafe.Application.Common;
using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared;

// The write-side authority on block payloads. Writes are strict: unknown types and malformed
// payloads are rejected, and what reaches the database is the canonical re-serialization of the
// typed payload plus its precomputed plain-text projection. Read-side tolerance for unknown
// types is a persistence concern and deliberately lives elsewhere.
public static class BlockPayloads
{
    public static TPayload DeserializeForRead<TPayload>(string canonicalJson)
        => JsonSerializer.Deserialize<TPayload>(canonicalJson, PayloadJson.Options)
            ?? throw new InvalidOperationException("A stored block payload cannot be null.");

    public static JsonElement SerializeForWrite<TPayload>(TPayload payload)
        => JsonSerializer.SerializeToElement(payload, PayloadJson.Options);

    public static Result<NormalizedBlockPayload> ValidateAndNormalize(string type, JsonElement content) => type switch
    {
        BlockTypes.Paragraph => NormalizeSpans<ParagraphPayload>(content, payload => payload.Spans),
        BlockTypes.Heading => NormalizeHeading(content),
        BlockTypes.Todo => NormalizeSpans<TodoPayload>(content, payload => payload.Spans),
        BlockTypes.Code => NormalizeCode(content),
        BlockTypes.Quote => NormalizeSpans<QuotePayload>(content, payload => payload.Spans),
        BlockTypes.Callout => NormalizeSpans<CalloutPayload>(content, payload => payload.Spans),
        BlockTypes.Divider => Normalize<DividerPayload>(
            content,
            static payload => Result.Success(payload),
            static _ => string.Empty
        ),
        BlockTypes.Table => NormalizeTable(content),
        BlockTypes.Image => NormalizeImage(content),
        BlockTypes.Audio => NormalizeAudio(content),
        _ => Result.Failure<NormalizedBlockPayload>(BlockErrors.UnsupportedBlockType)
    };

    private static Result<NormalizedBlockPayload> NormalizeSpans<TPayload>(JsonElement content, Func<TPayload, Spans?> spansOf)
        where TPayload : class
        => Normalize<TPayload>(
            content,
            payload => spansOf(payload) is null
                ? Result.Failure<TPayload>(BlockErrors.InvalidBlockPayload)
                : Result.Success(payload),
            payload => spansOf(payload)!.ToPlainText()
        );

    private static Result<NormalizedBlockPayload> NormalizeHeading(JsonElement content)
        => Normalize<HeadingPayload>(
            content,
            payload => payload.Spans is null || payload.Level is < 1 or > 6
                ? Result.Failure<HeadingPayload>(BlockErrors.InvalidBlockPayload)
                : Result.Success(payload),
            payload => payload.Spans.ToPlainText()
        );

    private static Result<NormalizedBlockPayload> NormalizeCode(JsonElement content)
        => Normalize<CodePayload>(
            content,
            payload => payload.Code is null || string.IsNullOrWhiteSpace(payload.Language)
                ? Result.Failure<CodePayload>(BlockErrors.InvalidBlockPayload)
                : Result.Success(payload with { Language = payload.Language.Trim().ToLowerInvariant() }),
            payload => payload.Code
        );

    private static Result<NormalizedBlockPayload> NormalizeTable(JsonElement content)
        => Normalize<TablePayload>(
            content,
            static payload =>
            {
                // `required` enforces presence, not non-null: explicit JSON nulls land here.
                if (payload.Alignments is null || payload.Alignments.Count == 0 || payload.Rows is null)
                {
                    return Result.Failure<TablePayload>(BlockErrors.InvalidBlockPayload);
                }

                var columns = payload.Alignments.Count;
                if (payload.Header is not null && payload.Header.Count != columns)
                {
                    return Result.Failure<TablePayload>(BlockErrors.InvalidBlockPayload);
                }

                if (payload.Rows.Any(row => row is null || row.Count != columns || row.Any(cell => cell is null)))
                {
                    return Result.Failure<TablePayload>(BlockErrors.InvalidBlockPayload);
                }

                return Result.Success(payload);
            },
            static payload => TablePlainText(payload)
        );

    // The search projection flattens every cell, header first; cell boundaries are not
    // meaningful for full-text matching.
    private static string TablePlainText(TablePayload payload)
    {
        var bodyCells = payload.Rows.SelectMany(row => row);
        var cells = payload.Header is null ? bodyCells : payload.Header.Concat(bodyCells);
        return string.Join(' ', cells.Select(cell => cell.ToPlainText()));
    }

    private static Result<NormalizedBlockPayload> NormalizeImage(JsonElement content)
        => Normalize<ImagePayload>(
            content,
            payload => string.IsNullOrWhiteSpace(payload.Url) || (!payload.IsDecorative && string.IsNullOrWhiteSpace(payload.Alt))
                ? Result.Failure<ImagePayload>(BlockErrors.InvalidBlockPayload)
                : Result.Success(payload),
            payload => payload.Alt ?? payload.Caption ?? string.Empty
        );

    private static Result<NormalizedBlockPayload> NormalizeAudio(JsonElement content)
        => Normalize<AudioPayload>(
            content,
            payload => string.IsNullOrWhiteSpace(payload.Url) || string.IsNullOrWhiteSpace(payload.MimeType)
                ? Result.Failure<AudioPayload>(BlockErrors.InvalidBlockPayload)
                : Result.Success(payload),
            static _ => string.Empty
        );

    private static Result<NormalizedBlockPayload> Normalize<TPayload>(
        JsonElement content,
        Func<TPayload, Result<TPayload>> validateAndNormalize,
        Func<TPayload, string> plainText
    )
        where TPayload : class
    {
        if (content.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return Result.Failure<NormalizedBlockPayload>(BlockErrors.InvalidBlockPayload);
        }

        TPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(content, PayloadJson.Options);
        }
        // Domain validation (mark constructors, Spans invariants) surfaces from the converter
        // pipeline as JsonException or ArgumentException; both mean the payload is invalid.
        catch (JsonException)
        {
            return Result.Failure<NormalizedBlockPayload>(BlockErrors.InvalidBlockPayload);
        }
        catch (ArgumentException)
        {
            return Result.Failure<NormalizedBlockPayload>(BlockErrors.InvalidBlockPayload);
        }

        if (payload is null)
        {
            return Result.Failure<NormalizedBlockPayload>(BlockErrors.InvalidBlockPayload);
        }

        var normalized = validateAndNormalize(payload);
        if (!normalized.IsSuccess)
        {
            return Result.Failure<NormalizedBlockPayload>(normalized.Error!);
        }

        var canonicalJson = JsonSerializer.Serialize(normalized.Value, PayloadJson.Options);
        return Result.Success(new NormalizedBlockPayload(canonicalJson, plainText(normalized.Value!)));
    }
}
