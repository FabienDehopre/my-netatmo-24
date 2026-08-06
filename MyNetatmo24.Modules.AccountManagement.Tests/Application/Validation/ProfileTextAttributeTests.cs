using System.ComponentModel.DataAnnotations;
using MyNetatmo24.Modules.AccountManagement.Application.Validation;

namespace MyNetatmo24.Modules.AccountManagement.Tests.Application.Validation;

public class ProfileTextAttributeTests
{
    private static ValidationResult? Validate(object? value) =>
        new ProfileTextAttribute().GetValidationResult(value, new ValidationContext(new object()) { MemberName = "Nickname", DisplayName = "Nickname" });

    [Test]
    [Arguments("Jane")]
    [Arguments("  Jane  ")]
    [Arguments("Ada Lovelace-King")]
    [Arguments("日花里")]
    [Arguments("Ковалевская")]
    [Arguments("مريم")]
    [Arguments("𝔍𝔞𝔫𝔢")]
    public async Task GetValidationResult_WithDisplayableText_Succeeds(string value)
    {
        await Assert.That(Validate(value)).IsNull();
    }

    [Test]
    public async Task GetValidationResult_WithNull_DefersToRequired()
    {
        await Assert.That(Validate(null)).IsNull();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("\t")]
    public async Task GetValidationResult_WithBlankText_Fails(string value)
    {
        await Assert.That(Validate(value)).IsNotNull();
    }

    [Test]
    public async Task GetValidationResult_WhenLongerThanTheMaximum_Fails()
    {
        await Assert.That(Validate(new string('a', ProfileTextAttribute.MaxLength + 1))).IsNotNull();
    }

    [Test]
    public async Task GetValidationResult_WhenTrimmingBringsItWithinTheMaximum_Succeeds()
    {
        await Assert.That(Validate($" {new string('a', ProfileTextAttribute.MaxLength)} ")).IsNull();
    }

    [Test]
    [Arguments(0x0000)] // Cc: null
    [Arguments(0x001B)] // Cc: escape
    [Arguments(0x200B)] // Cf: zero-width space
    [Arguments(0x200E)] // Cf: left-to-right mark
    [Arguments(0x2060)] // Cf: word joiner
    public async Task GetValidationResult_WithControlOrFormatCharacters_Fails(int codePoint)
    {
        var value = "Ja" + char.ConvertFromUtf32(codePoint) + "ne";

        await Assert.That(Validate(value)).IsNotNull();
    }

    [Test]
    public async Task GetValidationResult_NamesTheOffendingMember()
    {
        var result = Validate("   ");

        await Assert.That(result!.MemberNames).Contains("Nickname");
    }
}
