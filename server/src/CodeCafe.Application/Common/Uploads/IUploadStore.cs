namespace CodeCafe.Application.Common.Uploads;

/// <summary>
/// Persistent storage for content upload sessions. Supports chunked uploads for large files.
/// Implementations handle session creation, chunk appends, and cleanup.
/// </summary>
public interface IUploadStore
{
    Task<UploadStatus> CreateAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        CancellationToken cancellationToken
    );

    Task<UploadResult<UploadStatus>> CreateTextAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        string contentText,
        int maxUploadBytes,
        CancellationToken cancellationToken
    );

    Task<UploadResult<UploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken
    );

    Task<UploadResult<UploadSession>> GetAsync(
        Guid actorId,
        string uploadId,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken);
}

public sealed record UploadStatus(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

public sealed record UploadSession(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    string ContentText,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

public sealed record UploadError(string Code, string Message);

public sealed record UploadResult<T>(T? Value, UploadError? Error)
{
    public bool Succeeded => Error is null;

    public static UploadResult<T> Success(T value) => new(value, null);

    public static UploadResult<T> Failure(string code, string message) =>
        new(default, new UploadError(code, message));
}
