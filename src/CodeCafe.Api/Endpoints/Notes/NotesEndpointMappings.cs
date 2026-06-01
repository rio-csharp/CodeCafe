using CodeCafe.Application.Notes;

namespace CodeCafe.Api.Endpoints.Notes;

internal static class NotesEndpointMappings
{
    public static NotebookSummaryResponse ToSummaryResponse(NotebookSummaryModel model)
    {
        return new NotebookSummaryResponse(
            model.Id,
            model.OwnerId,
            model.Title,
            model.Slug,
            model.Description,
            model.Visibility,
            model.IsPublished,
            model.AuthorDisplayName,
            model.CanEdit,
            model.ItemCount,
            model.FolderCount,
            model.PageCount,
            model.FavoriteCount,
            model.IsFavoritedByMe,
            model.LastActivityAtUtc,
            model.CreatedAtUtc,
            model.UpdatedAtUtc,
            model.PublishedAtUtc);
    }

    public static NotebookDetailResponse ToDetailResponse(NotebookDetailModel model)
    {
        return new NotebookDetailResponse(
            model.Id,
            model.OwnerId,
            model.Title,
            model.Slug,
            model.Description,
            model.Visibility,
            model.IsPublished,
            model.AuthorDisplayName,
            model.CanEdit,
            model.ItemCount,
            model.FolderCount,
            model.PageCount,
            model.FavoriteCount,
            model.IsFavoritedByMe,
            model.LastActivityAtUtc,
            model.CreatedAtUtc,
            model.UpdatedAtUtc,
            model.PublishedAtUtc,
            model.Items.Select(ToItemResponse).ToList());
    }

    private static NotebookItemResponse ToItemResponse(NotebookItemModel model)
    {
        return new NotebookItemResponse(
            model.Id,
            model.NotebookId,
            model.ParentId,
            model.Type,
            model.Title,
            model.Slug,
            model.Path,
            model.SortOrder,
            model.ContentFormat,
            model.ContentJson,
            model.PlainTextContent,
            model.IsArchived,
            model.ArchivedAtUtc,
            model.ArchivedByUserId,
            model.CreatedAtUtc,
            model.UpdatedAtUtc);
    }
}
