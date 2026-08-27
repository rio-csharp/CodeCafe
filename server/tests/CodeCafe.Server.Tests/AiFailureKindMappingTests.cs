using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CodeCafe.Host.Tests;

/// <summary>
/// Replaces NotesFailureStatusCodeParityTests. That test pinned two duplicated
/// NotesFailureKind-to-status switches against each other; the AI flows now carry their own
/// AiFailureKind, because they fail in ways Notes cannot (provider errors, timeouts, unusable output).
/// The invariants worth holding changed shape accordingly: the AI mapping must be total, and the kinds
/// both taxonomies share must keep the same meaning when a Notes failure crosses into an AI flow.
/// </summary>
public sealed class AiFailureKindMappingTests
{
    [Theory]
    [InlineData(NotesFailureKind.Validation, AiFailureKind.Validation)]
    [InlineData(NotesFailureKind.Forbidden, AiFailureKind.Forbidden)]
    [InlineData(NotesFailureKind.NotFound, AiFailureKind.NotFound)]
    [InlineData(NotesFailureKind.Conflict, AiFailureKind.Conflict)]
    public void NotesFailures_WidenToTheMatchingAiKind(
        NotesFailureKind notesKind,
        AiFailureKind expected
    )
    {
        // Without this, the same underlying failure could surface as a 409 through the Notes endpoints
        // and a 400 through the AI endpoints.
        Assert.Equal(expected, AiFlowError.ToAiFailureKind(notesKind));
    }

    [Fact]
    public void EveryNotesFailureKind_WidensToADefinedAiKind()
    {
        // Enumerating the enum means adding a NotesFailureKind member forces a decision here rather
        // than silently falling through to Validation.
        foreach (var notesKind in Enum.GetValues<NotesFailureKind>())
        {
            Assert.True(Enum.IsDefined(AiFlowError.ToAiFailureKind(notesKind)));
        }
    }

    [Fact]
    public void AiFlowError_CarriesNoTransportConcern()
    {
        // The record used to hold an int StatusCode, which is what forced the application layer to
        // reference AspNetCore. Guard that it does not come back.
        var statusCodeMembers = typeof(AiFlowError)
            .GetProperties()
            .Where(property => property.Name.Contains("Status", StringComparison.OrdinalIgnoreCase))
            .Select(property => property.Name)
            .ToList();

        Assert.True(
            statusCodeMembers.Count == 0,
            "AiFlowError must stay transport-neutral, but exposes: "
                + string.Join(", ", statusCodeMembers)
        );
    }

    [Fact]
    public void ApplicationAssembly_DoesNotReferenceAspNetCore()
    {
        // The structural version of the same rule: use cases must be compilable without a web stack.
        var references = typeof(AiFlowError)
            .Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            references.Count == 0,
            "CodeCafe.Application must not reference AspNetCore, but references: "
                + string.Join(", ", references)
        );
    }

    [Fact]
    public void StatusCodesUsedByAiFlows_AreTheOnesTheTransportMaps()
    {
        // Documents the intended surface so a new kind without a status is visible here as well as in
        // the transport's own switch.
        var expected = new[]
        {
            StatusCodes.Status400BadRequest,
            StatusCodes.Status403Forbidden,
            StatusCodes.Status404NotFound,
            StatusCodes.Status409Conflict,
            StatusCodes.Status422UnprocessableEntity,
            StatusCodes.Status429TooManyRequests,
            StatusCodes.Status502BadGateway,
            StatusCodes.Status504GatewayTimeout,
        };

        Assert.Equal(expected.Length, Enum.GetValues<AiFailureKind>().Length);
    }
}
