namespace CodeCafe.Application.Notes;

public sealed record NoteSummary(
    string Path,
    string Title,
    DateTimeOffset UpdatedAt,
    long SizeBytes);
