using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Pages.PreparePageImport;

public sealed record PreparePageImportCommand(
    string NotebookIdOrSlug,
    string FileName,
    int MaxBytes,
    string? ParentPath = null
) : ICommand<Result<ImportUploadGrant>>;
