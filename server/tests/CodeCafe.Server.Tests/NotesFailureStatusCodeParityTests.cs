using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Notes.Application.Notes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CodeCafe.Server.Tests;

/// <summary>
/// The Ai module maps NotesFailureKind to a status code independently of Notes.Presentation, because
/// the mapping cannot be shared without either giving the framework-agnostic Notes.Application an
/// AspNetCore dependency or making Shared.Presentation depend on a business module. Duplication is the
/// lesser evil, but only while the copies agree: the same domain failure must not surface as a 409 on
/// one endpoint and a 400 on another.
/// </summary>
public sealed class NotesFailureStatusCodeParityTests
{
    [Theory]
    [InlineData(NotesFailureKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(NotesFailureKind.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(NotesFailureKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(NotesFailureKind.Conflict, StatusCodes.Status409Conflict)]
    public void AiHelpers_MapsEachFailureKind_ToTheExpectedStatus(NotesFailureKind kind, int expected)
    {
        Assert.Equal(expected, AiHelpers.ToStatusCode(kind));
    }

    [Fact]
    public void AiHelpers_CoversEveryFailureKind()
    {
        // A new NotesFailureKind that nobody maps would silently fall through to 400. This fails when
        // the enum grows, forcing a decision for the new member in both copies.
        var knownKinds = new[]
        {
            NotesFailureKind.Validation,
            NotesFailureKind.Forbidden,
            NotesFailureKind.NotFound,
            NotesFailureKind.Conflict
        };

        var declared = Enum.GetValues<NotesFailureKind>();

        Assert.Equal(
            knownKinds.OrderBy(kind => kind).ToArray(),
            declared.OrderBy(kind => kind).ToArray());
    }
}
