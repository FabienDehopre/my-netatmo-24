using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MyNetatmo24.SharedKernel.Infrastructure;
using MyNetatmo24.SharedKernel.StronglyTypedIds;
using MyNetatmo24.SharedKernel.Tests.TestSupport;

namespace MyNetatmo24.SharedKernel.Tests.Infrastructure;

public class ModuleDbContextTests
{
    private static readonly DateTimeOffset DeletionTime = new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    private static FakeTimeProvider TimeProvider() => new(DeletionTime);

    [Test]
    public async Task Schema_IsAppliedAsDefaultSchema()
    {
        await using var harness = await TestModuleDbContextHarness.CreateAsync(TimeProvider());

        var entityType = harness.Context.Model.FindEntityType(typeof(TestEntity))!;

        await Assert.That(entityType.GetSchema()).IsEqualTo("test");
    }

    [Test]
    public async Task VogenId_IsPersistedAndReadBack()
    {
        await using var harness = await TestModuleDbContextHarness.CreateAsync(TimeProvider());
        var id = AccountId.New();
        harness.Context.Entities.Add(new TestEntity { Id = id, Name = "sample" });
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        var reloaded = await harness.Context.Entities.SingleAsync();

        await Assert.That(reloaded.Id).IsEqualTo(id);
        await Assert.That(reloaded.Name).IsEqualTo("sample");
    }

    [Test]
    public async Task DeletingEntity_IsConvertedToSoftDelete()
    {
        await using var harness = await TestModuleDbContextHarness.CreateAsync(TimeProvider());
        var entity = new TestEntity { Id = AccountId.New(), Name = "to-delete" };
        harness.Context.Entities.Add(entity);
        await harness.Context.SaveChangesAsync();

        harness.Context.Entities.Remove(entity);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        // Still present in the table, but stamped as deleted rather than physically removed.
        var stored = await harness.Context.Entities
            .IgnoreQueryFilters([Constants.SoftDeleteFilter])
            .SingleAsync();
        await Assert.That(stored.DeletedAt).IsEqualTo(DeletionTime);
    }

    [Test]
    public async Task SoftDeletedEntities_AreHiddenByGlobalQueryFilter()
    {
        await using var harness = await TestModuleDbContextHarness.CreateAsync(TimeProvider());
        harness.Context.Entities.Add(new TestEntity { Id = AccountId.New(), Name = "active" });
        var deleted = new TestEntity { Id = AccountId.New(), Name = "deleted" };
        harness.Context.Entities.Add(deleted);
        await harness.Context.SaveChangesAsync();
        harness.Context.Entities.Remove(deleted);
        await harness.Context.SaveChangesAsync();
        harness.Context.ChangeTracker.Clear();

        var visible = await harness.Context.Entities.ToListAsync();

        await Assert.That(visible.Count).IsEqualTo(1);
        await Assert.That(visible[0].Name).IsEqualTo("active");
    }
}
