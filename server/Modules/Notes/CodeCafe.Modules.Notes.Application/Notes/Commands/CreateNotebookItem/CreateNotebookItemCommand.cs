using CodeCafe.Application.Common.Abstractions.Messaging;
using System.Text.Json;

namespace CodeCafe.Application.Notes.Commands.CreateNotebookItem;

public sealed record CreateNotebookItemCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    Guid? ParentId,
    string Type,
    string Title,
    int SortOrder,
    JsonElement? ContentJson) : ICommand<NotesResult<NotebookItemModel>>;
