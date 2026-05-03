namespace CodeCafe.Contracts.Notes;

public sealed record NotesSettingsResponse(string RootPath);

public sealed record UpsertNotesSettingsRequest(string RootPath);

public sealed record NoteSummaryResponse(
    string Path,
    string Title,
    DateTimeOffset UpdatedAt,
    long SizeBytes);

public sealed record NoteContentResponse(
    string Path,
    string Title,
    DateTimeOffset UpdatedAt,
    long SizeBytes,
    string Content);
