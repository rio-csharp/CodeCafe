using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Exceptions;
using CodeCafe.Domain.Identity;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresCollection))]
public sealed class UniqueConstraintTranslationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SaveChanges_TranslatesUniqueViolation_IntoTheApplicationException()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();

        var owner = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        dbContext.Users.Add(owner);
        dbContext.Notebooks.Add(Notebook.Create(owner.Id, "First", null, "taken-slug", NotebookVisibility.Private));
        dbContext.Notebooks.Add(Notebook.Create(owner.Id, "Second", null, "taken-slug", NotebookVisibility.Private));

        var exception = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal("IX_notebooks_Slug", exception.ConstraintName);
    }
}
