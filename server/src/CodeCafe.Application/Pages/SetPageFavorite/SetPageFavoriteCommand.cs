using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.SetPageFavorite;

public sealed record SetPageFavoriteCommand(Guid PageId, bool IsFavorite) : ICommand<Result>;
