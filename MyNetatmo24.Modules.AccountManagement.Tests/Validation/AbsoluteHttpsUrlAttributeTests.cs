using System.ComponentModel.DataAnnotations;
using MyNetatmo24.Modules.AccountManagement.Validation;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Validation;

public class AbsoluteHttpsUrlAttributeTests
{
    private const string MemberName = "AvatarUrl";

    private static ValidationResult? Validate(object? value, int maximumLength = 2048) =>
        new AbsoluteHttpsUrlAttribute(maximumLength).GetValidationResult(
            value,
            new ValidationContext(new object()) { MemberName = MemberName, DisplayName = MemberName });

    private static string UrlOfLength(int length)
    {
        const string prefix = "https://example.com/";
        return prefix + new string('a', length - prefix.Length);
    }

    [Test]
    public async Task GetValidationResult_WithAbsoluteHttpsUrl_Succeeds()
    {
        await Assert.That(Validate("https://example.com/jane.png")).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    public async Task GetValidationResult_WithNull_Succeeds()
    {
        // The avatar is optional; absence is not a rejection.
        await Assert.That(Validate(null)).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    [Arguments("http://example.com/jane.png")]
    [Arguments("ftp://example.com/jane.png")]
    public async Task GetValidationResult_WithoutTheHttpsScheme_Fails(string value)
    {
        var result = Validate(value);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    [Arguments("/jane.png")]
    [Arguments("jane.png")]
    [Arguments("")]
    public async Task GetValidationResult_WithRelativeUrl_Fails(string value)
    {
        var result = Validate(value);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    [Arguments("https://exa mple.com/jane.png")]
    [Arguments("https://")]
    public async Task GetValidationResult_WithTextNoUriParserAccepts_Fails(string value)
    {
        var result = Validate(value);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    public async Task GetValidationResult_WhenLongerThanTheMaximum_Fails()
    {
        var result = Validate(UrlOfLength(2049));

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    public async Task GetValidationResult_WhenExactlyTheMaximum_Succeeds()
    {
        await Assert.That(Validate(UrlOfLength(2048))).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    public async Task GetValidationResult_WithoutText_Throws()
    {
        // Applying the attribute to something that is not text is a programming error, not
        // something to report to whoever submitted the request.
        await Assert.That(() => Validate(new Uri("https://example.com/jane.png"))).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetValidationResult_MessageNamesTheField()
    {
        var result = Validate("http://example.com/jane.png");

        await Assert.That(result!.ErrorMessage).Contains(MemberName);
    }
}
