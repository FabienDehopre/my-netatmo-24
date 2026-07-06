using Microsoft.Extensions.Time.Testing;
using MyNetatmo24.SharedKernel.Domain;

namespace MyNetatmo24.SharedKernel.Tests.Domain;

public class SoftDeleteTests
{
    private sealed class Entity : ISoftDelete
    {
        public DateTimeOffset? DeletedAt { get; set; }
    }

    [Test]
    public async Task IsDeleted_WhenNotDeleted_IsFalse()
    {
        ISoftDelete entity = new Entity();

        await Assert.That(entity.IsDeleted).IsFalse();
    }

    [Test]
    public async Task Delete_SetsDeletedAtFromTimeProvider_AndMarksDeleted()
    {
        var now = new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        ISoftDelete entity = new Entity();

        entity.Delete(timeProvider);

        await Assert.That(entity.DeletedAt).IsEqualTo(now);
        await Assert.That(entity.IsDeleted).IsTrue();
    }

    [Test]
    public async Task Undo_ClearsDeletedAt()
    {
        ISoftDelete entity = new Entity { DeletedAt = DateTimeOffset.UtcNow };

        entity.Undo();

        await Assert.That(entity.DeletedAt).IsNull();
        await Assert.That(entity.IsDeleted).IsFalse();
    }
}
