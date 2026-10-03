namespace CodeCafe.Domain.Revisions;

// The lifecycle events recorded for every block mutation. The enum was pre-booked in the
// Application skeleton and lives in Domain now because the BlockRevision entity needs it.
public enum BlockChangeKind
{
    Added,
    Updated,
    Deleted,
    Moved
}
