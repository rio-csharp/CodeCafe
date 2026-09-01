namespace CodeCafe.Domain.Notes.ValueObjects;

public sealed record NotebookSlug
{
    public const int MaxLength = 180;
    public const int MinLength = 8;

    public string Value { get; }

    private NotebookSlug(string value) => Value = value;

    public static NotebookSlug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug must not be empty.", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Slug must not exceed {MaxLength} characters.", nameof(value));
        }

        return new NotebookSlug(normalized);
    }

    public override string ToString() => Value;
}
