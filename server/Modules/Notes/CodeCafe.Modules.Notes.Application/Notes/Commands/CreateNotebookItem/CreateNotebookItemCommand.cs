using CodeCafe.Shared.Application.Common.Abstractions.Messaging;
using System.Text.Json;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebookItem;

public sealed record CreateNotebookItemCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    Guid? ParentId,
    string Type,
    string Title,
    int SortOrder,
    JsonElement? ContentJson) : ICommand<NotesResult<NotebookItemModel>>;
