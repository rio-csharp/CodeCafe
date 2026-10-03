using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Host.Hosting;

// Scoped ambient change source: plain HTTP writes stay at the Human default; MCP tools flip it
// to Ai for the duration of their call, so the revision log attributes machine edits correctly.
internal sealed class AmbientChangeSource : IChangeSourceAccessor
{
    public RevisionSource Source { get; set; } = RevisionSource.Human;
}
