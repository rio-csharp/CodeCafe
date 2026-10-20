using System.ComponentModel;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.PrepareNotebookImport;
using CodeCafe.Application.Pages.PreparePageImport;
using CodeCafe.Host.Hosting;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class ImportMcpTools
{
    // Cheap on purpose: the result is a few hundred bytes, and calling it tells a client how
    // the import endpoints are shaped without handing out a credential.
    [McpServerTool(Name = "codecafe_get_import_recipe", ReadOnly = true, Idempotent = true)]
    [Description(
        "Get the import endpoint shapes and size limits. This tool never returns a credential: "
            + "for an actual upload call codecafe_prepare_page_import or "
            + "codecafe_prepare_notebook_import, which returns a short-lived token scoped to one target."
    )]
    public static CallToolResult GetImportRecipe(
        IHttpContextAccessor httpContextAccessor,
        IOptions<McpOptions> mcpOptions
    ) => McpToolResults.Success(ImportGuidance.Recipe(httpContextAccessor.HttpContext, mcpOptions.Value));

    [McpServerTool(Name = "codecafe_prepare_page_import")]
    [Description(
        "Get a short-lived upload URL and token for importing a local markdown file as a new page. "
            + "Use this instead of passing a large file through codecafe_import_page: the file goes "
            + "straight from disk to the API and never enters the conversation. The token is scoped to "
            + "the notebook, parent path, file name, and size, and expires in a few minutes. Run the "
            + "returned command with the local file path, then report the created page."
    )]
    public static async Task<CallToolResult> PreparePageImport(
        ISender sender,
        IImportUploadTokenService tokens,
        IHttpContextAccessor httpContextAccessor,
        string notebookIdOrSlug,
        string fileName,
        CancellationToken cancellationToken,
        string? parentPath = null
    )
    {
        var prepared = await sender.Send(
            new PreparePageImportCommand(notebookIdOrSlug, fileName, RequestLimits.ImportBytes, parentPath),
            cancellationToken
        );
        if (!prepared.IsSuccess)
        {
            return McpToolResults.Failure(prepared.Error!);
        }

        return McpToolResults.Success(
            ImportUploadResponse.Build(prepared.Value!, tokens, httpContextAccessor.HttpContext, "pages")
        );
    }

    [McpServerTool(Name = "codecafe_prepare_notebook_import")]
    [Description(
        "Get a short-lived upload URL and token for importing a local markdown file as a new notebook. "
            + "Same flow as codecafe_prepare_page_import: the token is scoped to the file name and "
            + "size, expires in a few minutes, and the file never enters the conversation."
    )]
    public static async Task<CallToolResult> PrepareNotebookImport(
        ISender sender,
        IImportUploadTokenService tokens,
        IHttpContextAccessor httpContextAccessor,
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var prepared = await sender.Send(
            new PrepareNotebookImportCommand(fileName, RequestLimits.ImportBytes),
            cancellationToken
        );
        if (!prepared.IsSuccess)
        {
            return McpToolResults.Failure(prepared.Error!);
        }

        return McpToolResults.Success(
            ImportUploadResponse.Build(prepared.Value!, tokens, httpContextAccessor.HttpContext, "notebooks")
        );
    }
}
