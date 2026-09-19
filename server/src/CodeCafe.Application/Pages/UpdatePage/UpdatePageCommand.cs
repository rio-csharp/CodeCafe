using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.UpdatePage;

public sealed record UpdatePageCommand(Guid PageId, string? Title, bool? IsArchived)
    : ICommand<Result<PageDetailsDto>>;
