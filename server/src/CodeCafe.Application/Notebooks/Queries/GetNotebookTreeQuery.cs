using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Queries;

public sealed record GetNotebookTreeQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookTreeDto>>;
