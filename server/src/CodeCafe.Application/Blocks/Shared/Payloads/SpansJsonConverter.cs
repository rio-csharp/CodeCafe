using System.Text.Json;
using System.Text.Json.Serialization;

using CodeCafe.Domain.Blocks;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

// Bridges Spans (a validating value object with a private constructor) to its flat array wire
// shape. Reading canonicalizes through Spans.Create, so a payload that passes deserialization
// already satisfies every span invariant; writing emits the canonical list.
internal sealed class SpansJsonConverter : JsonConverter<Spans>
{
    public override Spans Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // "spans": null is not the empty text; the only legal empty shape is [].
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("Spans must be an array, not null.");
        }

        var spans = JsonSerializer.Deserialize<List<Span>>(ref reader, options);
        return Spans.Create(spans ?? []);
    }

    public override void Write(Utf8JsonWriter writer, Spans value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.Items, options);
}
