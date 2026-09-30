namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record AudioPayload
{
    public required string Url { get; init; }

    public required string MimeType { get; init; }

    public long? Duration { get; init; }
}
