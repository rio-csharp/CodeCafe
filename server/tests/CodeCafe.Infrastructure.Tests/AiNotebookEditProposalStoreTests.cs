using System.Text.Json;
using CodeCafe.Application.Ai;
using CodeCafe.Infrastructure.Ai;

namespace CodeCafe.Infrastructure.Tests;

public sealed class AiNotebookEditProposalStoreTests
{
    [Fact]
    public async Task Save_Then_TryGet_ReturnsProposalWithSamePayload()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        await store.SaveAsync(proposal, CancellationToken.None);

        var loaded = await store.TryGetAsync(proposal.ProposalId, actorId, CancellationToken.None);
        Assert.NotNull(loaded);
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
        Assert.Equal(
            proposal.BeforeContentJson!.Value.GetRawText(),
            loaded.BeforeContentJson!.Value.GetRawText()
        );
        Assert.Equal(proposal.BeforePlainTextContent, loaded.BeforePlainTextContent);
        Assert.Equal(proposal.AfterContentJson.GetRawText(), loaded.AfterContentJson.GetRawText());
        Assert.Equal(proposal.AfterPlainTextContent, loaded.AfterPlainTextContent);
        Assert.Equal(
            proposal.OperationsJson!.Value.GetRawText(),
            loaded.OperationsJson!.Value.GetRawText()
        );
        Assert.Equal(proposal.SourcePageUpdatedAtUtc, loaded.SourcePageUpdatedAtUtc);
        Assert.Equal(proposal.GeneratedAtUtc, loaded.GeneratedAtUtc);
        Assert.Equal(proposal.ExpiresAtUtc, loaded.ExpiresAtUtc);
        Assert.Equal(proposal.Summary, loaded.Summary);
    }

    [Fact]
    public async Task TryGet_UsesSeparatePersistenceScope_SoOtherReplicasSeeTheProposal()
    {
        using var harness = new NotesDbHarness();
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        using (var createContext = harness.CreateContext())
        {
            await new DatabaseAiNotebookEditProposalStore(createContext).SaveAsync(
                proposal,
                CancellationToken.None
            );
        }

        using (var readContext = harness.CreateContext())
        {
            var loaded = await new DatabaseAiNotebookEditProposalStore(readContext).TryGetAsync(
                proposal.ProposalId,
                actorId,
                CancellationToken.None
            );
            Assert.NotNull(loaded);
            Assert.Equal(proposal.ProposalId, loaded.ProposalId);
        }
    }

    [Fact]
    public async Task TryGet_ReturnsFalse_ForDifferentActor()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var proposal = CreateProposal(Guid.NewGuid());
        await store.SaveAsync(proposal, CancellationToken.None);

        var loaded = await store.TryGetAsync(
            proposal.ProposalId,
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryGet_ReturnsFalse_ForUnknownProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);

        var loaded = await store.TryGetAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryGet_ReturnsFalse_ForExpiredProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId, DateTimeOffset.UtcNow.AddMinutes(-1));
        await store.SaveAsync(proposal, CancellationToken.None);

        var loaded = await store.TryGetAsync(proposal.ProposalId, actorId, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task TryConsumeAsync_ReturnsNull_ForExpiredProposal_And_KeepsIt()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId, DateTimeOffset.UtcNow.AddMinutes(-1));
        await store.SaveAsync(proposal, CancellationToken.None);

        var consumed = await store.TryConsumeAsync(
            proposal.ProposalId,
            actorId,
            CancellationToken.None
        );

        Assert.Null(consumed);
        Assert.Contains(context.AiEditProposals, entry => entry.Id == proposal.ProposalId);
    }

    [Fact]
    public async Task TryConsumeAsync_ReturnsProposal_And_RemovesItFromStore()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);
        await store.SaveAsync(proposal, CancellationToken.None);

        var consumed = await store.TryConsumeAsync(
            proposal.ProposalId,
            actorId,
            CancellationToken.None
        );

        Assert.NotNull(consumed);
        Assert.Equal(proposal.ProposalId, consumed.ProposalId);
        Assert.Equal(proposal.Summary, consumed.Summary);
        Assert.Null(await store.TryGetAsync(proposal.ProposalId, actorId, CancellationToken.None));
        Assert.DoesNotContain(context.AiEditProposals, entry => entry.Id == proposal.ProposalId);
    }

    [Fact]
    public async Task TryConsumeAsync_CalledTwice_SecondCallReturnsNull()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);
        await store.SaveAsync(proposal, CancellationToken.None);

        var first = await store.TryConsumeAsync(
            proposal.ProposalId,
            actorId,
            CancellationToken.None
        );
        var second = await store.TryConsumeAsync(
            proposal.ProposalId,
            actorId,
            CancellationToken.None
        );

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task TryConsumeAsync_ReturnsNull_ForDifferentActor_And_KeepsProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);
        await store.SaveAsync(proposal, CancellationToken.None);

        var consumed = await store.TryConsumeAsync(
            proposal.ProposalId,
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Null(consumed);
        Assert.NotNull(
            await store.TryGetAsync(proposal.ProposalId, actorId, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Remove_DeletesProposal()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);
        await store.SaveAsync(proposal, CancellationToken.None);

        await store.RemoveAsync(proposal.ProposalId, CancellationToken.None);

        Assert.Null(await store.TryGetAsync(proposal.ProposalId, actorId, CancellationToken.None));
    }

    [Fact]
    public async Task Remove_IsIdempotent_WhenProposalIsAbsent()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);

        await store.RemoveAsync(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task Save_PrunesExpiredProposals()
    {
        using var harness = new NotesDbHarness();
        using var context = harness.CreateContext();
        var store = new DatabaseAiNotebookEditProposalStore(context);
        var expired = CreateProposal(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));
        await store.SaveAsync(expired, CancellationToken.None);

        var valid = CreateProposal(Guid.NewGuid());
        await store.SaveAsync(valid, CancellationToken.None);

        Assert.DoesNotContain(context.AiEditProposals, entry => entry.Id == expired.ProposalId);
        Assert.Contains(context.AiEditProposals, entry => entry.Id == valid.ProposalId);
    }

    [Fact]
    public async Task Create_Get_Discard_Flow_WorksAcrossScopes()
    {
        using var harness = new NotesDbHarness();
        var actorId = Guid.NewGuid();
        var proposal = CreateProposal(actorId);

        using (var createContext = harness.CreateContext())
        {
            await new DatabaseAiNotebookEditProposalStore(createContext).SaveAsync(
                proposal,
                CancellationToken.None
            );
        }

        using (var readContext = harness.CreateContext())
        {
            Assert.NotNull(
                await new DatabaseAiNotebookEditProposalStore(readContext).TryGetAsync(
                    proposal.ProposalId,
                    actorId,
                    CancellationToken.None
                )
            );
        }

        using (var discardContext = harness.CreateContext())
        {
            await new DatabaseAiNotebookEditProposalStore(discardContext).RemoveAsync(
                proposal.ProposalId,
                CancellationToken.None
            );
        }

        using (var verifyContext = harness.CreateContext())
        {
            Assert.Null(
                await new DatabaseAiNotebookEditProposalStore(verifyContext).TryGetAsync(
                    proposal.ProposalId,
                    actorId,
                    CancellationToken.None
                )
            );
        }
    }

    private static AiNotebookEditProposal CreateProposal(
        Guid actorId,
        DateTimeOffset? expiresAtUtc = null
    )
    {
        using var beforeDocument = JsonDocument.Parse(
            """{"type":"doc","content":[{"type":"paragraph"}]}"""
        );
        using var afterDocument = JsonDocument.Parse(
            """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"hello"}]}]}"""
        );
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
            "Update 'folder/page'."
        );
    }
}
