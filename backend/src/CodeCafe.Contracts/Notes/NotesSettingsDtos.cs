namespace CodeCafe.Contracts.Notes;

public sealed record NotesSettingsResponse(string RootPath);

public sealed record UpsertNotesSettingsRequest(string RootPath);
