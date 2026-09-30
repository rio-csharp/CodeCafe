namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record ImagePayload
{
    public required string Url { get; init; }

    // Alt text is mandatory for meaningful images; purely decorative images opt out explicitly.
    public string? Alt { get; init; }

    public bool IsDecorative { get; init; }

    public string? Caption { get; init; }
}
