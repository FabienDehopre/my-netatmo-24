using System.ComponentModel.DataAnnotations;

namespace MyNetatmo24.Modules.AccountManagement.Validation;

internal static class ValidationContextExtensions
{
    extension(ValidationContext validationContext)
    {
        /// <summary>
        /// Builds a rejection attached to the member being validated, so that the response names
        /// the field the person has to correct.
        /// </summary>
        /// <param name="message">The reason the value was rejected.</param>
        /// <returns>The failed validation result.</returns>
        public ValidationResult Failure(string message) =>
            validationContext.MemberName is { } memberName
                ? new ValidationResult(message, [memberName])
                : new ValidationResult(message);
    }
}
