using AlFariq.Domain;

namespace AlFariq.Domain.Tests;

public class EntityTests
{
    private sealed class Sample : Entity;

    [Fact]
    public void New_entities_get_a_unique_non_empty_guid()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => new Sample().Id).ToList();

        Assert.DoesNotContain(Guid.Empty, ids);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Ids_are_version_7_so_they_sort_by_creation_time()
    {
        var id = new Sample().Id;

        Assert.Equal(7, id.Version);
    }

    [Fact]
    public void IsDeleted_follows_DeletedAt()
    {
        var entity = new Sample();
        Assert.False(entity.IsDeleted);

        entity.DeletedAt = DateTimeOffset.UtcNow;
        Assert.True(entity.IsDeleted);
    }
}
