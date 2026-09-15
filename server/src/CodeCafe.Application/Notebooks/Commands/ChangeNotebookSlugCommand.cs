using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Commands;

public sealed record ChangeNotebookSlugCommand(string NotebookIdOrSlug, string NewSlug)
    : ICommand<Result<NotebookDetailsDto>>;
