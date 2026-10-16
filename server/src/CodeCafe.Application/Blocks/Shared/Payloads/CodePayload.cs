namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record CodePayload
{
    public required string Code { get; init; }

    // Free text, normalized to lowercase by the registry.
    public required string Language { get; init; }
}
