using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.ChangePageSlug;

// A slug is the caller's pick, so a clash is theirs to fix; leaving it out asks the server to
// derive one from the page title, which is how the reader repairs a page that was still called
// "Untitled" when it picked up its URL.
public sealed record ChangePageSlugCommand(Guid PageId, string? Slug) : ICommand<Result<PageDetailsDto>>;
