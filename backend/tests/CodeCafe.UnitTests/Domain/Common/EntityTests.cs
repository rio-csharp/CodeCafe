using CodeCafe.Domain.Common;

namespace CodeCafe.UnitTests.Domain.Common;

public sealed class EntityTests
{
    [Fact]
    public void Entity_creates_non_empty_identifier_by_default()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    private sealed class TestEntity : Entity;
}
