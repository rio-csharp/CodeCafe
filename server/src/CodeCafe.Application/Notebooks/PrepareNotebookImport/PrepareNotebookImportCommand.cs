using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Notebooks.PrepareNotebookImport;

public sealed record PrepareNotebookImportCommand(string FileName, int MaxBytes)
    : ICommand<Result<ImportUploadGrant>>;
