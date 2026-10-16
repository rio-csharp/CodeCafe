using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.ImportPage;

public sealed record ImportPageCommand(string NotebookIdOrSlug, PageExportDto Export, string? ParentPath = null)
    : ICommand<Result<PageDetailsDto>>;
