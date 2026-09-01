namespace CodeCafe.Domain.Notes.ValueObjects;

public sealed record NotebookPath
{
    public const int MaxLength = 1024;

    public string Value { get; }

    private NotebookPath(string value) => Value = value;

    public static NotebookPath Create(string value)
    {
        var normalized = value.Trim().Trim('/');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Path must not be empty.", nameof(value));
        }

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Path must not exceed {MaxLength} characters.", nameof(value));
        }

        return new NotebookPath(normalized);
    }

    public bool IsDescendantOf(NotebookPath parent) =>
        Value.StartsWith(parent.Value + "/", StringComparison.Ordinal);

    public static int GetSlugBudget(string? parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return Math.Min(NotebookSlug.MaxLength, MaxLength);
        }

        var remaining = MaxLength - parentPath.Length - 1;
        return remaining < NotebookSlug.MinLength ? 0 : Math.Min(NotebookSlug.MaxLength, remaining);
    }

    public static bool HasRoomForChild(string? parentPath) => GetSlugBudget(parentPath) > 0;

    public override string ToString() => Value;
}
