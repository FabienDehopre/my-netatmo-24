using MyNetatmo24.SharedKernel.Infrastructure;

namespace MyNetatmo24.SharedKernel.Tests.Infrastructure;

public class HelperExtensionsTests
{
    [Test]
    public async Task ThrowIfNull_WhenValueIsNotNull_ReturnsSameInstance()
    {
        var value = new object();

        await Assert.That(value.ThrowIfNull()).IsSameReferenceAs(value);
    }

    [Test]
    public async Task ThrowIfNull_WhenValueIsNull_ThrowsWithParameterName()
    {
        object? value = null;

        await Assert.That(() => value.ThrowIfNull())
            .Throws<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Test]
    public async Task ThrowIfNullOrEmpty_WhenValueIsValid_ReturnsSameValue()
    {
        await Assert.That("hello".ThrowIfNullOrEmpty()).IsEqualTo("hello");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task ThrowIfNullOrEmpty_WhenValueIsNullOrEmpty_Throws(string? value)
    {
        await Assert.That(() => value.ThrowIfNullOrEmpty()).Throws<ArgumentException>();
    }

    [Test]
    public async Task ThrowIfNullOrWhiteSpace_WhenValueIsValid_ReturnsSameValue()
    {
        await Assert.That("hello".ThrowIfNullOrWhiteSpace()).IsEqualTo("hello");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ThrowIfNullOrWhiteSpace_WhenValueIsBlank_Throws(string? value)
    {
        await Assert.That(() => value.ThrowIfNullOrWhiteSpace()).Throws<ArgumentException>();
    }
}
