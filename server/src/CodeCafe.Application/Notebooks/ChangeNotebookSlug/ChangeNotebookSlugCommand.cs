using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;

namespace CodeCafe.Application.Notebooks.ChangeNotebookSlug;

public sealed record ChangeNotebookSlugCommand(string NotebookIdOrSlug, string NewSlug)
    : ICommand<Result<NotebookDetailsDto>>;
