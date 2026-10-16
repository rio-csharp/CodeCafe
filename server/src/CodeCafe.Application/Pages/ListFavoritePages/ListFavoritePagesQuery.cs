using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.ListFavoritePages;

public sealed record ListFavoritePagesQuery(Guid? NotebookId = null) : IQuery<Result<IReadOnlyList<FavoritePageDto>>>;
