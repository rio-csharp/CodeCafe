using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.ExportPage;

public sealed record ExportPageQuery(Guid PageId, string? AccessCode = null) : IQuery<Result<PageExportDto>>;
