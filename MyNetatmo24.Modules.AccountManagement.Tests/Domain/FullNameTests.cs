using MyNetatmo24.Modules.AccountManagement.Domain;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Domain;

public class FullNameTests
{
    [Test]
    public async Task From_WithValidNames_CreatesFullName()
    {
        var fullName = FullName.From("John", "Doe");

        await Assert.That(fullName.FirstName).IsEqualTo("John");
        await Assert.That(fullName.LastName).IsEqualTo("Doe");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task From_WithInvalidFirstName_Throws(string? firstName)
    {
        await Assert.That(() => FullName.From(firstName, "Doe")).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task From_WithInvalidLastName_Throws(string? lastName)
    {
        await Assert.That(() => FullName.From("John", lastName)).Throws<ArgumentException>();
    }

    [Test]
    public async Task ToString_ReturnsFirstAndLastNameSeparatedBySpace()
    {
        var fullName = FullName.From("John", "Doe");

        await Assert.That(fullName.ToString()).IsEqualTo("John Doe");
    }
}
