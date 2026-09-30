using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class NotebookBlockTransferTests
{
    [Fact]
    public async Task Export_IncludesPageAndBlockMarkdown()
    {
        var owner = User.Create("owner@example.com", "Owner", "User", "hash");
        var notebook = Notebook.Create(owner.Id, "Notes", null, "notes", NotebookVisibility.Private);
        var page = Page.Create(notebook.Id, null, "Getting Started", "getting-started", "a");
        var block = Block.Create(
            page.Id,
            null,
            "paragraph",
            "{\"spans\":[{\"text\":\"Hello\"}]}",
            "Hello",
            "a"
        );
        BlockChain.Insert(block, page, null, null, null);
        notebook.SetFirstPage(page.Id);

        var notebooks = new StubNotebookRepository { notebook };
        var pages = new StubPageRepository(notebooks) { page };
        var blocks = new StubBlockRepository { block };
        var handler = new ExportNotebookQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            notebooks,
            pages,
            blocks,
            new StubPasswordHasher()
        );

        var result = await handler.Handle(new ExportNotebookQuery("notes"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("notes.md", result.Value!.FileName);
        Assert.Contains("# Notes", result.Value.Markdown);
        Assert.Contains("## Getting Started", result.Value.Markdown);
        Assert.Contains("Hello", result.Value.Markdown);
    }

    [Fact]
    public async Task Import_CalloutWithoutBodyLine_KeepsFollowingContent()
    {
        var owner = User.Create("owner@example.com", "Owner", "User", "hash");
        var users = new StubUserRepository { owner };
        var notebooks = new StubNotebookRepository();
        var pages = new StubPageRepository(notebooks);
        var blocks = new StubBlockRepository();
        var handler = new ImportNotebookCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            users,
            notebooks,
            pages,
            blocks,
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new ImportNotebookCommand(
                new NotebookExportDto("notes.md", "# Notes\n\n## Page\n\n> [!WARNING]\nKeep me")
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, blocks.Count);
        Assert.Contains(blocks, block => block.Type == "callout" && block.PlainText == string.Empty);
        Assert.Contains(blocks, block => block.Type == "paragraph" && block.PlainText == "Keep me");
    }

    [Fact]
    public async Task Import_LeavesUnclosedInlineMarkersAsLiteralText()
    {
        var owner = User.Create("owner@example.com", "Owner", "User", "hash");
        var users = new StubUserRepository { owner };
        var notebooks = new StubNotebookRepository();
        var pages = new StubPageRepository(notebooks);
        var blocks = new StubBlockRepository();
        var handler = new ImportNotebookCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            users,
            notebooks,
            pages,
            blocks,
            new StubUnitOfWork()
        );

        var result = await handler.Handle(
            new ImportNotebookCommand(new NotebookExportDto("notes.md", "# Notes\n\n## Page\n\n*unfinished `code")),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("*unfinished `code", Assert.Single(blocks).PlainText);
    }

    [Fact]
    public async Task Import_CreatesNotebookPagesAndBlocks()
    {
        var owner = User.Create("owner@example.com", "Owner", "User", "hash");
        var users = new StubUserRepository { owner };
        var notebooks = new StubNotebookRepository();
        var pages = new StubPageRepository(notebooks);
        var blocks = new StubBlockRepository();
        var unitOfWork = new StubUnitOfWork();
        var handler = new ImportNotebookCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(owner.Id)),
            users,
            notebooks,
            pages,
            blocks,
            unitOfWork
        );

        var result = await handler.Handle(
            new ImportNotebookCommand(
                new NotebookExportDto(
                    "imported.md",
                    "# Imported\n\n## First Page\n\nHello **world**\n\n- [x] Done"
                )
            ),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        var notebook = Assert.Single(notebooks);
        Assert.Equal("Imported", notebook.Title);
        Assert.Equal("imported", notebook.Slug);
        Assert.Single(pages);
        Assert.Equal(2, blocks.Count);
        Assert.Contains(blocks, block => block.Type == "paragraph" && block.PlainText == "Hello world");
        Assert.Contains(blocks, block => block.Type == "todo" && block.PlainText == "Done");
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
