namespace CodeCafe.Application.Notes;

public sealed record NoteContent(
    string Path,
    string Title,
    DateTimeOffset UpdatedAt,
    long SizeBytes,
    string Content);
