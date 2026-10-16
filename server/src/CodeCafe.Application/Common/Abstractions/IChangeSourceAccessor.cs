using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Common.Abstractions;

// Ambient attribution for the revision log, mirroring ICurrentUserAccessor: the Host boundary
// sets it per request (MCP tool calls mark themselves Ai), handlers only read it. Scoped; the
// default is Human, so plain HTTP writes never touch it.
public interface IChangeSourceAccessor
{
    RevisionSource Source { get; set; }
}
