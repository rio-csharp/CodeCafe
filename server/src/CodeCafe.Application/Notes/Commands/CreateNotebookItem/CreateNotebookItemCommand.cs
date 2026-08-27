using System.Text.Json;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.CreateNotebookItem;

public sealed record CreateNotebookItemCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    Guid? ParentId,
    string Type,
    string Title,
    int SortOrder,
    JsonElement? ContentJson
) : ICommand<NotesResult<NotebookItemModel>>;
