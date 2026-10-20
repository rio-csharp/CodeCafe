using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Pages.ExportPage;
using CodeCafe.Application.Pages.ImportPage;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Revisions;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCafe.Host.Controllers;

// The HTTP half of the MCP import flow: the caller holds a short-lived token scoped to one
// target, not the MCP client's long-term bearer. The target comes from the token, so the
// multipart body carries only the file.
//
// The upload scheme is not the only gate: a normal PAT or access token can still satisfy the
// authenticated-user policy through the default scheme, so each action checks for the grant
// item before it reads the file or touches the database.
[ApiController]
[Route("api/import-uploads")]
[Authorize(AuthenticationSchemes = ImportUploadAuthenticationHandler.SchemeName)]
[EnableRateLimiting(RateLimiterExtensions.ImportUploadPolicy)]
[Tags("Imports")]
public sealed class ImportUploadsController(ISender sender, IChangeSourceAccessor changeSource) : ControllerBase
{
    [HttpPost("pages")]
    [RequestSizeLimit(RequestLimits.ImportUploadRequestBytes)]
    [Consumes("multipart/form-data")]
    public async Task<Result<PageDetailsDto>> ImportPage(IFormFile file, CancellationToken cancellationToken)
    {
        if (HttpContext.GetImportUploadGrant() is not { } grant)
        {
            return Result.Failure<PageDetailsDto>(ImportUploadErrors.MissingGrant);
        }
        if (grant.Kind != ImportUploadKind.Page || grant.NotebookId is not { } notebookId)
        {
            return Result.Failure<PageDetailsDto>(ImportUploadErrors.WrongKind);
        }
        if (file.Length > grant.MaxBytes)
        {
            return Result.Failure<PageDetailsDto>(ImportUploadErrors.TooLarge(grant.MaxBytes));
        }

        var markdown = await file.ReadMarkdownAsync(cancellationToken);
        changeSource.Source = RevisionSource.Ai;
        return await sender.Send(
            new ImportPageCommand(
                notebookId.ToString(),
                new PageExportDto(grant.FileName, markdown),
                grant.ParentPath
            ),
            cancellationToken
        );
    }

    [HttpPost("notebooks")]
    [RequestSizeLimit(RequestLimits.ImportUploadRequestBytes)]
    [Consumes("multipart/form-data")]
    public async Task<Result<NotebookDetailsDto>> ImportNotebook(IFormFile file, CancellationToken cancellationToken)
    {
        if (HttpContext.GetImportUploadGrant() is not { } grant)
        {
            return Result.Failure<NotebookDetailsDto>(ImportUploadErrors.MissingGrant);
        }
        if (grant.Kind != ImportUploadKind.Notebook)
        {
            return Result.Failure<NotebookDetailsDto>(ImportUploadErrors.WrongKind);
        }
        if (file.Length > grant.MaxBytes)
        {
            return Result.Failure<NotebookDetailsDto>(ImportUploadErrors.TooLarge(grant.MaxBytes));
        }

        var markdown = await file.ReadMarkdownAsync(cancellationToken);
        changeSource.Source = RevisionSource.Ai;
        return await sender.Send(
            new ImportNotebookCommand(new NotebookExportDto(grant.FileName, markdown)),
            cancellationToken
        );
    }
}
