namespace CodeCafe.Api.Authorization;

public sealed class NotesSettingsEditRequirementHandler(IHostEnvironment environment)
    : AuthorizationHandler<NotesSettingsEditRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotesSettingsEditRequirement requirement)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
