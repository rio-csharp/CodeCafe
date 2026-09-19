using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookDetails;

namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

public sealed record GetNotebookDetailsQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookDetailsDto>>;
