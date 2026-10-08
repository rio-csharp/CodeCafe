namespace CodeCafe.Application.Blocks.Shared;

// The initial type catalog. The wire form is the lowercase name; BlockPayloads is the
// validation authority that maps these to their typed payloads.
public static class BlockTypes
{
    public const string Paragraph = "paragraph";

    public const string Heading = "heading";

    public const string Todo = "todo";

    public const string Code = "code";

    public const string Quote = "quote";

    public const string Callout = "callout";

    public const string BulletedList = "bulleted-list";

    public const string NumberedList = "numbered-list";

    public const string Divider = "divider";

    public const string Table = "table";

    public const string Image = "image";

    public const string Audio = "audio";
}
