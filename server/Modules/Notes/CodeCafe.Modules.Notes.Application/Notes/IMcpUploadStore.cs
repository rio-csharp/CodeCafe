namespace CodeCafe.Modules.Notes.Application.Notes;

public interface IMcpUploadStore
{
    Task<McpUploadStatus> CreateAsync(Guid actorId, string? fileName, string mediaType, CancellationToken cancellationToken);

    Task<NotesUploadResult<McpUploadStatus>> CreateTextAsync(
        Guid actorId,
        string? fileName,
        string mediaType,
        string contentText,
        int maxUploadBytes,
        CancellationToken cancellationToken);

    Task<NotesUploadResult<McpUploadStatus>> AppendTextAsync(
        Guid actorId,
        string uploadId,
        string chunkText,
        int maxChunkBytes,
        int maxUploadBytes,
        CancellationToken cancellationToken);

    Task<NotesUploadResult<McpUploadSession>> GetAsync(Guid actorId, string uploadId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid actorId, string uploadId, CancellationToken cancellationToken);
}

public sealed record McpUploadStatus(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record McpUploadSession(
    string UploadId,
    Guid ActorId,
    string? FileName,
    string MediaType,
    string ContentText,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record NotesUploadError(string Code, string Message);

public sealed record NotesUploadResult<T>(T? Value, NotesUploadError? Error)
{
    public bool Succeeded => Error is null;

    public static NotesUploadResult<T> Success(T value) => new(value, null);

    public static NotesUploadResult<T> Failure(string code, string message) => new(default, new NotesUploadError(code, message));
}
