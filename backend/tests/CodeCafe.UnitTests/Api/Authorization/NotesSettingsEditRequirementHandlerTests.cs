using CodeCafe.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace CodeCafe.UnitTests.Api.Authorization;

public sealed class NotesSettingsEditRequirementHandlerTests
{
    [Fact]
    public async Task Handle_requirement_succeeds_in_development()
    {
        var requirement = NotesSettingsEditRequirement.Instance;
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);
        var handler = new NotesSettingsEditRequirementHandler(new StubHostEnvironment("Development"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_requirement_succeeds_in_testing()
    {
        var requirement = NotesSettingsEditRequirement.Instance;
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);
        var handler = new NotesSettingsEditRequirementHandler(new StubHostEnvironment("Testing"));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_requirement_does_not_succeed_outside_editable_environments()
    {
        var requirement = NotesSettingsEditRequirement.Instance;
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), resource: null);
        var handler = new NotesSettingsEditRequirementHandler(new StubHostEnvironment("Production"));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "CodeCafe";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
