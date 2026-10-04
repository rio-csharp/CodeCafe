namespace CodeCafe.Application.Common;

// Shared by the notebook and page favorite endpoints; single-feature requests live in their
// feature folder instead.
public sealed record SetFavoriteRequest(bool IsFavorite);
