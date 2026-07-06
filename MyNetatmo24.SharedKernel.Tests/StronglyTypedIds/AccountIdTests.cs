using MyNetatmo24.SharedKernel.StronglyTypedIds;
using Vogen;

namespace MyNetatmo24.SharedKernel.Tests.StronglyTypedIds;

public class AccountIdTests
{
    [Test]
    public async Task New_ProducesNonEmptyUniqueValues()
    {
        var first = AccountId.New();
        var second = AccountId.New();

        await Assert.That(first.Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(first).IsNotEqualTo(second);
    }

    [Test]
    public async Task From_WithValidGuid_WrapsTheValue()
    {
        var guid = Guid.CreateVersion7();

        var id = AccountId.From(guid);

        await Assert.That(id.Value).IsEqualTo(guid);
    }

    [Test]
    public async Task From_WithEmptyGuid_ThrowsValidationException()
    {
        await Assert.That(() => AccountId.From(Guid.Empty)).Throws<ValueObjectValidationException>();
    }
}
