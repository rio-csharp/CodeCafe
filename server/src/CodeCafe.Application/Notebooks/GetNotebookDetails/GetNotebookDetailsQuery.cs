using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

public sealed record GetNotebookDetailsQuery(string NotebookIdOrSlug, string? AccessCode = null)
    : IQuery<Result<NotebookDetailsDto>>;
