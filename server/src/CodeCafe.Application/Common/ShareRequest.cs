using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Common;

// Shared by the notebook and page share endpoints; single-feature requests live in their
// feature folder instead.
public sealed record ShareRequest(
    string Email,
    CollaboratorRole Role);
