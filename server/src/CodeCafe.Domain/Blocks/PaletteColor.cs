using System.Text.Json.Serialization;

namespace CodeCafe.Domain.Blocks;

// Closed palette shared by color/highlight marks and callout variants; the wire form is the
// lowercase name, so free-text colors never reach the database.
[JsonConverter(typeof(JsonStringEnumConverter<PaletteColor>))]
public enum PaletteColor
{
    [JsonStringEnumMemberName("primary")]
    Primary,

    [JsonStringEnumMemberName("success")]
    Success,

    [JsonStringEnumMemberName("danger")]
    Danger,

    [JsonStringEnumMemberName("warning")]
    Warning,

    [JsonStringEnumMemberName("info")]
    Info,

    [JsonStringEnumMemberName("muted")]
    Muted
}
