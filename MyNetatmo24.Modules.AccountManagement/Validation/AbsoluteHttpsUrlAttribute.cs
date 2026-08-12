using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MyNetatmo24.Modules.AccountManagement.Validation;

/// <summary>
/// Validates that submitted text is an absolute <c>https</c> URL of a bounded length.
/// </summary>
/// <remarks>
/// <para>
/// The application never fetches the URL itself -- it is handed to the identity provider and later
/// rendered in a browser -- so an absolute location is required (a relative one would resolve
/// against whatever page displays it) and the scheme is pinned to <c>https</c> so that a profile
/// picture cannot downgrade the page that shows it. The length bound keeps a data URL or a padded
/// query string from being smuggled in as an avatar.
/// </para>
/// <para>
/// The value is validated as the text it was submitted as, rather than as an already-parsed
/// <see cref="Uri"/>: text no URI parser accepts would otherwise be refused while the request body
/// is still being read, which answers 400 without naming the field that has to be corrected.
/// </para>
/// </remarks>
/// <param name="maximumLength">
/// The maximum number of characters the submitted URL may contain.
/// </param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AbsoluteHttpsUrlAttribute(int maximumLength) : ValidationAttribute
{
    /// <summary>
    /// Gets the maximum number of characters the submitted URL may contain.
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

        // A missing value is the business of [Required]; an optional URL is simply absent.
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string text)
        {
            throw new InvalidOperationException($"{nameof(AbsoluteHttpsUrlAttribute)} can only validate text, but {validationContext.DisplayName} is a {value.GetType()}.");
        }

        var name = validationContext.DisplayName;

        // Measured on the submitted text rather than on the parsed form, so that the person gets
        // the verdict on what they actually typed.
        if (text.Length > MaximumLength)
        {
            var maximumLength = MaximumLength.ToString(CultureInfo.InvariantCulture);
            return validationContext.Failure($"The {name} field must be at most {maximumLength} characters long.");
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var url))
        {
            return validationContext.Failure($"The {name} field must be an absolute URL.");
        }

        return string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            ? ValidationResult.Success
            : validationContext.Failure($"The {name} field must be an https URL.");
    }
}
