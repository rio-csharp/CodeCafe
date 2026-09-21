using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.GetNotebookSlugAvailability;

public sealed record GetNotebookSlugAvailabilityQuery(string Slug)
    : IQuery<Result<NotebookSlugAvailabilityDto>>;
