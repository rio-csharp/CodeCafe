using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCafe.Application.Blocks.Shared.Payloads;

// One strict options instance for the whole payload registry: unknown JSON members are rejected
// (write-strict), names are the web-style camelCase the API speaks, and Spans flows through its
// validating value object.
internal static class PayloadJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new SpansJsonConverter() }
    };
}
