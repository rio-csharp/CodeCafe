using CodeCafe.Application.Notes;

namespace CodeCafe.WebApi.Mcp;

public sealed record McpMutationResult<T>
    where T : class
{
    private McpMutationResult(
        T? value,
        NotesError? error,
        string? successText,
        Guid? notebookId,
        Guid? itemId)
    {
        Value = value;
        Error = error;
        SuccessText = successText;
        NotebookId = notebookId;
        ItemId = itemId;
    }

    public T? Value { get; }

    public NotesError? Error { get; }

    public string? SuccessText { get; }

    public Guid? NotebookId { get; }

    public Guid? ItemId { get; }

    public bool Succeeded => Error is null;

    public static McpMutationResult<T> Success(
        T value,
        string successText,
        Guid? notebookId,
        Guid? itemId)
        => new(value, error: null, successText, notebookId, itemId);

    public static McpMutationResult<T> Failure(
        NotesError error,
        Guid? notebookId = null,
        Guid? itemId = null)
        => new(value: null, error, successText: null, notebookId, itemId);
}
