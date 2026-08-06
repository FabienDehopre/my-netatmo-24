using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace MyNetatmo24.Modules.AccountManagement.Application.Validation;

/// <summary>
/// Validates a short, displayable profile value - a nickname or a name part. The value is judged on its
/// trimmed form: it must not be blank, must stay within <see cref="MaxLength"/> characters, and must not
/// contain Unicode control or format characters. Any script is welcome otherwise, so that people whose
/// name is not written in Latin are not asked to romanize it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ProfileTextAttribute : TextFieldAttribute
{
    /// <summary>
    /// The maximum number of characters a profile value may hold once trimmed.
    /// </summary>
    public const int MaxLength = 50;

    protected override int MaxAllowedLength => MaxLength;

    protected override ValidationResult? IsValid(string text, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return Invalid(validationContext, "must not be blank.");
        }

        if (ValidateLength(trimmed, validationContext) is { } tooLong)
        {
            return tooLong;
        }

        // Enumerating runes rather than chars keeps characters outside the BMP intact.
        foreach (var rune in trimmed.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return Invalid(validationContext, "must not contain control or formatting characters.");
            }
        }

        return ValidationResult.Success;
    }
}
