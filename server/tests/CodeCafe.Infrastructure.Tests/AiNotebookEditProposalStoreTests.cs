using CodeCafe.Modules.Ai.Edits;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Tests;

public sealed class AiNotebookEditProposalStoreTests
{
    [Fact]
    public void Save_Then_TryGet_ReturnsProposalWithSamePayload()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        store.Save(proposal);

        var found = store.TryGet(proposal.ProposalId, actorId, out var loaded);
        Assert.True(found);
        Assert.Equal(proposal.ProposalId, loaded.ProposalId);
        Assert.Equal(proposal.ActorId, loaded.ActorId);
        Assert.Equal(proposal.RequestedOperation, loaded.RequestedOperation);
        Assert.Equal(proposal.EffectiveOperation, loaded.EffectiveOperation);
        Assert.Equal(proposal.Mode, loaded.Mode);
        Assert.Equal(proposal.NotebookId, loaded.NotebookId);
        Assert.Equal(proposal.NotebookSlug, loaded.NotebookSlug);
        Assert.Equal(proposal.NotebookTitle, loaded.NotebookTitle);
        Assert.Equal(proposal.PageId, loaded.PageId);
        Assert.Equal(proposal.Title, loaded.Title);
        Assert.Equal(proposal.PagePath, loaded.PagePath);
        Assert.Equal(proposal.ParentPath, loaded.ParentPath);
        Assert.Equal(proposal.BeforeContentJson!.Value.GetRawText(), loaded.BeforeContentJson!.Value.GetRawText());
        Assert.Equal(proposal.BeforePlainTextContent, loaded.BeforePlainTextContent);
        Assert.Equal(proposal.AfterContentJson.GetRawText(), loaded.AfterContentJson.GetRawText());
        Assert.Equal(proposal.AfterPlainTextContent, loaded.AfterPlainTextContent);
        Assert.Equal(proposal.OperationsJson!.Value.GetRawText(), loaded.OperationsJson!.Value.GetRawText());
        Assert.Equal(proposal.SourcePageUpdatedAtUtc, loaded.SourcePageUpdatedAtUtc);
        Assert.Equal(proposal.GeneratedAtUtc, loaded.GeneratedAtUtc);
        Assert.Equal(proposal.ExpiresAtUtc, loaded.ExpiresAtUtc);
        Assert.Equal(proposal.Summary, loaded.Summary);
    }

    [Fact]
    public void TryGet_UsesSeparatePersistenceScope_SoOtherReplicasSeeTheProposal()
    {
        using var harness = new NotesDbHarness();
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        using (var createContext = harness.CreateContext())
        {
            new DatabaseAiNotebookEditProposalStore(createContext).Save(proposal);
        }

        using (var readContext = harness.CreateContext())
        {
            var found = new DatabaseAiNotebookEditProposalStore(readContext).TryGet(proposal.ProposalId, actorId, out var loaded);
            Assert.True(found);
            Assert.Equal(proposal.ProposalId, loaded.ProposalId);
        }
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForDifferentActor()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var proposal = CreateProposal(Guid.NewGuid());
        store.Save(proposal);

        var found = store.TryGet(proposal.ProposalId, Guid.NewGuid(), out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);

        var found = store.TryGet(Guid.NewGuid(), Guid.NewGuid(), out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForExpiredProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId, DateTimeOffset.UtcNow.AddMinutes(-1));
        store.Save(proposal);

        var found = store.TryGet(proposal.ProposalId, actorId, out _);

        Assert.False(found);
    }

    [Fact]
    public void Remove_DeletesProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);
        store.Save(proposal);

        store.Remove(proposal.ProposalId);

        Assert.False(store.TryGet(proposal.ProposalId, actorId, out _));
    }

    [Fact]
    public void Remove_IsIdempotent_WhenProposalIsAbsent()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);

        store.Remove(Guid.NewGuid());
    }

    [Fact]
    public void Save_PrunesExpiredProposals()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var expired = CreateProposal(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));
        store.Save(expired);

        var valid = CreateProposal(Guid.NewGuid());
        store.Save(valid);

        Assert.DoesNotContain(context.AiEditProposals, entry => entry.Id == expired.ProposalId);
        Assert.Contains(context.AiEditProposals, entry => entry.Id == valid.ProposalId);
    }

    [Fact]
    public void Create_Get_Discard_Flow_WorksAcrossScopes()
    {
        using var harness = new NotesDbHarness();
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        using (var createContext = harness.CreateContext())
        {
            new DatabaseAiNotebookEditProposalStore(createContext).Save(proposal);
        }

        using (var readContext = harness.CreateContext())
        {
            Assert.True(new DatabaseAiNotebookEditProposalStore(readContext).TryGet(proposal.ProposalId, actorId, out _));
        }

        using (var discardContext = harness.CreateContext())
        {
            new DatabaseAiNotebookEditProposalStore(discardContext).Remove(proposal.ProposalId);
        }

        using (var verifyContext = harness.CreateContext())
        {
            Assert.False(new DatabaseAiNotebookEditProposalStore(verifyContext).TryGet(proposal.ProposalId, actorId, out _));
        }
    }

    private static AiNotebookEditProposal CreateProposal(Guid actorId, DateTimeOffset? expiresAtUtc = null)
    {
        using var beforeDocument = JsonDocument.Parse("""{"type":"doc","content":[{"type":"paragraph"}]}""");
        using var afterDocument = JsonDocument.Parse("""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"hello"}]}]}""");
        using var operationsDocument = JsonDocument.Parse("""[{"op":"replace","index":0}]""");

        return new AiNotebookEditProposal(
            Guid.NewGuid(),
            actorId,
            "auto",
            "replace_current_page",
            "content",
            Guid.NewGuid(),
            "notebook-slug",
            "Notebook Title",
            Guid.NewGuid(),
            "Page Title",
            "folder/page",
            "folder",
            beforeDocument.RootElement.Clone(),
            "before text",
            afterDocument.RootElement.Clone(),
            "hello",
            operationsDocument.RootElement.Clone(),
            DateTimeOffset.Parse("2026-06-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-01T00:05:00+00:00"),
            expiresAtUtc ?? DateTimeOffset.UtcNow.AddMinutes(30),
            "Update 'folder/page'.");
    }
}
