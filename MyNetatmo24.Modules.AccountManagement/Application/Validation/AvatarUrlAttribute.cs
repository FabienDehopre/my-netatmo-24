using System.ComponentModel.DataAnnotations;

namespace MyNetatmo24.Modules.AccountManagement.Application.Validation;

/// <summary>
/// Validates an optional avatar URL: when supplied it must be an absolute <c>https</c> URI of at most
/// <see cref="MaxLength"/> characters. A blank value counts as "no avatar" rather than as an error, so
/// that an empty form field and an omitted one behave the same.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AvatarUrlAttribute : TextFieldAttribute
{
    /// <summary>
    /// The maximum number of characters an avatar URL may hold.
    /// </summary>
    public const int MaxLength = 2048;

    protected override int MaxAllowedLength => MaxLength;

    protected override ValidationResult? IsValid(string text, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        if (ValidateLength(text, validationContext) is { } tooLong)
        {
            return tooLong;
        }

        return Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? ValidationResult.Success
            : Invalid(validationContext, "must be an absolute https URL.");
    }
}
