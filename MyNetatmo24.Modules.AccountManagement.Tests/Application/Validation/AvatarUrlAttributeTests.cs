using System.ComponentModel.DataAnnotations;
using MyNetatmo24.Modules.AccountManagement.Application.Validation;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application.Validation;

public class AvatarUrlAttributeTests
{
    private static ValidationResult? Validate(object? value) =>
        new AvatarUrlAttribute().GetValidationResult(value, new ValidationContext(new object()) { MemberName = "AvatarUrl", DisplayName = "AvatarUrl" });

    [Test]
    [Arguments("https://example.com/avatar.png")]
    [Arguments("https://example.com/a/very/deep/path?with=query#and-fragment")]
    public async Task GetValidationResult_WithAbsoluteHttpsUrl_Succeeds(string value)
    {
        await Assert.That(Validate(value)).IsNull();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task GetValidationResult_WithoutAValue_Succeeds(string? value)
    {
        await Assert.That(Validate(value)).IsNull();
    }

    [Test]
    [Arguments("http://example.com/avatar.png")]
    [Arguments("ftp://example.com/avatar.png")]
    [Arguments("javascript:alert(1)")]
    [Arguments("/avatar.png")]
    [Arguments("example.com/avatar.png")]
    [Arguments("not a url at all")]
    public async Task GetValidationResult_WithAnythingButAnAbsoluteHttpsUrl_Fails(string value)
    {
        await Assert.That(Validate(value)).IsNotNull();
    }

    [Test]
    public async Task GetValidationResult_WhenLongerThanTheMaximum_Fails()
    {
        var url = "https://example.com/" + new string('a', AvatarUrlAttribute.MaxLength);

        await Assert.That(Validate(url)).IsNotNull();
    }

    [Test]
    public async Task GetValidationResult_NamesTheOffendingMember()
    {
        var result = Validate("http://example.com/avatar.png");

        await Assert.That(result!.MemberNames).Contains("AvatarUrl");
    }
}
