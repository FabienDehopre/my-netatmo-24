using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace MyNetatmo24.Modules.AccountManagement.Validation;

/// <summary>
/// Validates a free-text profile value a person picked for themselves (a nickname, a given name, a
/// family name) as it will be displayed back to them.
/// </summary>
/// <remarks>
/// <para>
/// The value is judged on its trimmed form, which is also the form that is stored: surrounding
/// whitespace is a typing accident, not input the person meant to submit. Once trimmed it must not
/// be empty, must fit in <see cref="MaximumLength"/> characters, and must contain no Unicode
/// control (Cc) or format (Cf) character.
/// </para>
/// <para>
/// Every script is accepted otherwise: nobody has to romanize their own name to use the
/// application. Rejecting Cc/Cf is about display sanity -- invisible characters that let two
/// different values look identical, or reorder the text around them -- and not an escaping
/// measure, which the frontend already performs on render. The trade-off is that legitimate
/// sequences relying on format characters, such as an emoji joined with U+200D or Persian text
/// spelled with U+200C, are refused as well.
/// </para>
/// </remarks>
/// <param name="maximumLength">
/// The maximum number of characters the trimmed value may contain.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ProfileTextAttribute(int maximumLength) : ValidationAttribute
{
    /// <summary>
    /// Gets the maximum number of characters the trimmed value may contain.
    /// </summary>
    public int MaximumLength { get; } = maximumLength;

    /// <inheritdoc/>
    /// <remarks>
    /// The rejection has to name the member it is about, so the context-free overloads of
    /// <see cref="ValidationAttribute"/> cannot serve this attribute.
    /// </remarks>
    public override bool RequiresValidationContext => true;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The attribute was applied to a member that does not hold text.
    /// </exception>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        // A missing value is the business of [Required]; reporting it here too would name the same
        // field twice with two different messages.
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string text)
        {
            throw new InvalidOperationException($"{nameof(ProfileTextAttribute)} can only validate text, but {validationContext.DisplayName} is a {value.GetType()}.");
        }

        var name = validationContext.DisplayName;
        var trimmed = text.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return validationContext.Failure($"The {name} field must not be empty.");
        }

        if (trimmed.Length > MaximumLength)
        {
            var maximumLength = MaximumLength.ToString(CultureInfo.InvariantCulture);
            return validationContext.Failure($"The {name} field must be at most {maximumLength} characters long.");
        }

        if (ContainsControlOrFormatCharacter(trimmed))
        {
            return validationContext.Failure($"The {name} field must not contain control or formatting characters.");
        }

        return ValidationResult.Success;
    }

    private static bool ContainsControlOrFormatCharacter(ReadOnlySpan<char> text)
    {
        // Enumerated as runes so that a character outside the Basic Multilingual Plane is
        // categorized as the single character it is, rather than as its two surrogate halves.
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }
}
