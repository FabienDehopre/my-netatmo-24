using MyNetatmo24.SharedKernel.Messages;
using MyNetatmo24.SharedKernel.StronglyTypedIds;

namespace MyNetatmo24.SharedKernel.Tests.Messages;

public class MessagesTests
{
    [Test]
    public async Task AccountCreated_ExposesItsMembers()
    {
        var id = AccountId.New();

        var message = new AccountCreated(id, "John", "Doe");

        await Assert.That(message.Id).IsEqualTo(id);
        await Assert.That(message.FirstName).IsEqualTo("John");
        await Assert.That(message.LastName).IsEqualTo("Doe");
    }

    [Test]
    public async Task AccountRestored_ExposesItsMembers()
    {
        var id = AccountId.New();

        var message = new AccountRestored(id);

        await Assert.That(message.Id).IsEqualTo(id);
    }
}
