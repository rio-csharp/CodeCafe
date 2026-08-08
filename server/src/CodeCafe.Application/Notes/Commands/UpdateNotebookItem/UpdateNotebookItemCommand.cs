using CodeCafe.Application.Common.Messaging;
using System.Text.Json;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebookItem;

public sealed record UpdateNotebookItemCommand(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId,
    string Title,
    JsonElement ParentId,
    int? SortOrder,
    JsonElement ContentJson,
    DateTimeOffset? ExpectedUpdatedAtUtc = null) : ICommand<NotesResult<NotebookItemModel>>;
