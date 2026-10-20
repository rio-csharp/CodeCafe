using CodeCafe.Application.Common.Security;
using CodeCafe.Host.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Tests.Mcp;

// The tool methods are discovered by reflection, and the SDK decides per parameter whether it
// comes from DI or from the model's arguments using IServiceProviderIsService. Neither is
// exercised at start-up, so a parameter the container cannot supply would silently become a
// required tool argument and fail only when a client called the tool.
public sealed class McpToolSurfaceTests(CodeCafeFactory factory) : IClassFixture<CodeCafeFactory>
{
    [Fact]
    public void ImportTools_AreRegistered()
    {
        var names = factory
            .Services.GetServices<McpServerTool>()
            .Select(tool => tool.ProtocolTool.Name)
            .ToList();

        Assert.Contains("codecafe_get_import_recipe", names);
        Assert.Contains("codecafe_prepare_page_import", names);
        Assert.Contains("codecafe_prepare_notebook_import", names);
        Assert.Contains("codecafe_import_notebook", names);
        Assert.Contains("codecafe_import_page", names);
    }

    [Fact]
    public void ImportTools_ParametersResolveFromTheContainer()
    {
        var isService = factory.Services.GetRequiredService<IServiceProviderIsService>();

        // Resolved as services rather than as tool arguments, which is what keeps them out of
        // the schema the model sees.
        Assert.True(isService.IsService(typeof(IHttpContextAccessor)));
        Assert.True(isService.IsService(typeof(IOptions<McpOptions>)));
        Assert.True(isService.IsService(typeof(IImportUploadTokenService)));

        // The recipe is built from configuration the deployment owns, so the bound value has to
        // be there for the advertised limit to match what the tools enforce.
        var options = factory.Services.GetRequiredService<IOptions<McpOptions>>().Value;
        Assert.True(options.MaxInlineContentBytes > 0);
    }
}
