using System.ComponentModel.DataAnnotations;
using MyNetatmo24.Modules.AccountManagement.Validation;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Validation;

public class ProfileTextAttributeTests
{
    private const string MemberName = "Nickname";

    private static ValidationResult? Validate(object? value, int maximumLength = 50) =>
        new ProfileTextAttribute(maximumLength).GetValidationResult(
            value,
            new ValidationContext(new object()) { MemberName = MemberName, DisplayName = MemberName });

    [Test]
    public async Task GetValidationResult_WithPlainText_Succeeds()
    {
        await Assert.That(Validate("Jane")).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    public async Task GetValidationResult_WithNull_Succeeds()
    {
        // A missing value is the business of [Required], not of this attribute.
        await Assert.That(Validate(null)).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    [Arguments("  Jane  ")]
    [Arguments("\tJane\n")]
    public async Task GetValidationResult_WithSurroundingWhitespace_Succeeds(string value)
    {
        await Assert.That(Validate(value)).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    [Arguments("山田")]
    [Arguments("Ольга")]
    [Arguments("أحمد")]
    [Arguments("Ægir Þórsson")]
    [Arguments("Jean-Luc O'Brien")]
    public async Task GetValidationResult_WithNonLatinScript_Succeeds(string value)
    {
        // Any script is accepted: nobody has to romanize their own name.
        await Assert.That(Validate(value)).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("\t\n")]
    public async Task GetValidationResult_WhenEmptyAfterTrimming_Fails(string value)
    {
        var result = Validate(value);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    public async Task GetValidationResult_WhenLongerThanTheMaximumAfterTrimming_Fails()
    {
        var result = Validate($"  {new string('a', 51)}  ");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    public async Task GetValidationResult_WhenExactlyTheMaximumAfterTrimming_Succeeds()
    {
        await Assert.That(Validate($"  {new string('a', 50)}  ")).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    [Arguments("Jane\u0007Doe")] // BELL, category Cc
    [Arguments("Jane\u001bDoe")] // ESCAPE, category Cc
    [Arguments("Jane\u00adDoe")] // SOFT HYPHEN, category Cf
    [Arguments("Jane\u200dDoe")] // ZERO WIDTH JOINER, category Cf
    [Arguments("Jane\u202eDoe")] // RIGHT-TO-LEFT OVERRIDE, category Cf
    public async Task GetValidationResult_WithControlOrFormatCharacter_Fails(string value)
    {
        var result = Validate(value);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.MemberNames).Contains(MemberName);
    }

    [Test]
    public async Task GetValidationResult_WithAstralPlaneCharacters_Succeeds()
    {
        // Surrogate pairs must be read as the single character they encode, not as two
        // uncategorized halves.
        await Assert.That(Validate("Jane \U0001f31e")).IsEqualTo(ValidationResult.Success);
    }

    [Test]
    public async Task GetValidationResult_WithoutText_Throws()
    {
        // Applying the attribute to something that is not text is a programming error, not
        // something to report to whoever submitted the request.
        await Assert.That(() => Validate(42)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetValidationResult_MessageNamesTheField()
    {
        var result = Validate("   ");

        await Assert.That(result!.ErrorMessage).Contains(MemberName);
    }
}
