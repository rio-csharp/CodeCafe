namespace CodeCafe.Application.Blocks.Shared.Payloads;

public sealed record AudioPayload
{
    public required string Url { get; init; }

    public required string MimeType { get; init; }

    // Playback length in whole seconds (the only plausible writer is the client's
    // HTMLMediaElement.duration, which reports seconds), or null when unknown -- and unknown is
    // the normal state, since nothing populates this yet. The reader renders a native
    // <audio controls> that measures the clip itself; this field is for callers that need the
    // length without loading the media, such as search results, export, or the assistant.
    public long? Duration { get; init; }
}
