namespace CodeCafe.Domain.Revisions;

// Who made a change. Ai is attributed through IChangeSourceAccessor at the Host boundary (MCP
// tools), everything else defaults to Human.
public enum RevisionSource
{
    Human,
    Ai
}
