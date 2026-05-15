namespace CodeCafe.Api.Authorization;

public sealed class NotesSettingsEditRequirement : IAuthorizationRequirement
{
    public static NotesSettingsEditRequirement Instance { get; } = new();

    private NotesSettingsEditRequirement()
    {
    }
}
