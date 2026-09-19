using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodeCafe.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public sealed class UserRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Add_Save_And_Find_RoundTrips()
    {
        var user = User.Create("Yao@Example.com", "yao@example.com", "Yao", "password-hash");

        await using (var dbContext = await fixture.CreateCleanContextAsync())
        {
            var repository = new UserRepository(dbContext);
            await repository.AddAsync(user, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var verifyContext = await fixture.CreateContextAsync();
        var verifyRepository = new UserRepository(verifyContext);

        var found = await verifyRepository.FindByEmailAsync("yao@example.com", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(user.Id, found.Id);
        Assert.Equal("Yao@Example.com", found.Email);
        Assert.Equal("yao@example.com", found.NormalizedEmail);
        Assert.Equal("Yao", found.DisplayName);
        Assert.Equal("password-hash", found.PasswordHash);
        // PostgreSQL timestamptz stores microseconds, not .NET ticks.
        Assert.Equal(user.CreatedAtUtc, found.CreatedAtUtc, TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task FindByEmail_ReturnsNull_WhenNoUserMatches()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var repository = new UserRepository(dbContext);

        var found = await repository.FindByEmailAsync("nobody@example.com", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task Duplicate_NormalizedEmail_ViolatesUniqueIndex()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var repository = new UserRepository(dbContext);
        await repository.AddAsync(
            User.Create("first@example.com", "first@example.com", "First", "hash"),
            CancellationToken.None
        );
        await dbContext.SaveChangesAsync(CancellationToken.None);

        await repository.AddAsync(
            User.Create("FIRST@example.com", "first@example.com", "Second", "hash"),
            CancellationToken.None
        );
        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync(CancellationToken.None)
        );

        var postgresException = Assert.IsAssignableFrom<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }
}
