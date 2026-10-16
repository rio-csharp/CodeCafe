using System.Text.Json.Serialization;

namespace CodeCafe.Domain.Blocks;

// Per-column alignment for table blocks; the wire form is the lowercase name, mirroring
// PaletteColor. None renders as a plain "---" delimiter in markdown exports.
[JsonConverter(typeof(JsonStringEnumConverter<TableColumnAlignment>))]
public enum TableColumnAlignment
{
    [JsonStringEnumMemberName("none")]
    None,

    [JsonStringEnumMemberName("left")]
    Left,

    [JsonStringEnumMemberName("center")]
    Center,

    [JsonStringEnumMemberName("right")]
    Right
}
