using System.ComponentModel.DataAnnotations;

namespace MyNetatmo24.Modules.AccountManagement.Application.Validation;

/// <summary>
/// Shared plumbing for the attributes that judge a single displayable text field. Every one of them
/// repeats the same opening moves - a missing value is <see cref="RequiredAttribute"/>'s business, a
/// non-string value can only be reported as such, and the value has a maximum length - and every one of
/// them has to shape its failures so they name the field they are about. Subclasses inherit all of that
/// and are left with the rule that actually distinguishes them.
/// </summary>
public abstract class TextFieldAttribute : ValidationAttribute
{
    /// <summary>
    /// The maximum number of characters the validated value may hold. Which form of the value is
    /// measured - raw or trimmed - is the subclass's decision, made when it calls
    /// <see cref="ValidateLength"/>.
    /// </summary>
    protected abstract int MaxAllowedLength { get; }

    /// <summary>
    /// Judges a value already known to be a non-null string.
    /// </summary>
    /// <param name="text">The submitted value.</param>
    /// <param name="validationContext">The context describing the field the value came from.</param>
    /// <returns><see cref="ValidationResult.Success"/> when the value is acceptable; a failure otherwise.</returns>
    protected abstract ValidationResult? IsValid(string text, ValidationContext validationContext);

    /// <summary>
    /// Reports <paramref name="text"/> as too long when it exceeds <see cref="MaxAllowedLength"/>.
    /// </summary>
    /// <param name="text">The form of the value the subclass wants measured.</param>
    /// <param name="validationContext">The context describing the field the value came from.</param>
    /// <returns><see cref="ValidationResult.Success"/> when the value fits; a failure otherwise.</returns>
    protected ValidationResult? ValidateLength(string text, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.Length > MaxAllowedLength
            ? Invalid(validationContext, $"must be at most {MaxAllowedLength} characters long.")
            : ValidationResult.Success;
    }

    /// <summary>
    /// Builds a failure that names both the field and what is wrong with it, so a caller reading the
    /// validation problem knows which input to fix.
    /// </summary>
    /// <param name="validationContext">The context describing the field the value came from.</param>
    /// <param name="problem">The complaint, phrased to follow "The {field} field ".</param>
    /// <returns>The failure.</returns>
    protected static ValidationResult Invalid(ValidationContext validationContext, string problem)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        return new ValidationResult(
            $"The {validationContext.DisplayName} field {problem}",
            validationContext.MemberName is { } memberName ? [memberName] : []);
    }

    protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        // A missing value is the business of [Required], not of this attribute.
        if (value is null)
        {
            return ValidationResult.Success;
        }

        return value is string text
            ? IsValid(text, validationContext)
            : Invalid(validationContext, "must be a text value.");
    }
}
