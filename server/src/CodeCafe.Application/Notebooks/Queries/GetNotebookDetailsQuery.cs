using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Queries;

public sealed record GetNotebookDetailsQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookDetailsDto>>;
