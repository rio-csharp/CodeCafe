using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record SetNotebookFavoriteCommand(string NotebookIdOrSlug, bool IsFavorite) : ICommand<Result>;
