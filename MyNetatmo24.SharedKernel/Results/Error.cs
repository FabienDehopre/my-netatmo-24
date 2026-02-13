using System.Diagnostics.CodeAnalysis;

namespace MyNetatmo24.SharedKernel.Results;

#pragma warning disable CA1716
public record Error(string Code, string Message)
#pragma warning restore CA1716
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static NotFoundError NotFound(string Code, string Message) => new(Code, Message);
    public static ValidationError Validation(string Code, string Message) => new(Code, Message);
    public static ConflictError Conflict(string Code, string Message) => new(Code, Message);

    public static UnauthorizedError Unauthorized(string Code, string Message) => new(Code, Message);

    // ... other error types as needed
    public static Error Failure(string Code, string Message) => new(Code, Message);
    public static Error Failure([NotNull] Exception exception) => new(exception.GetType().Name, exception.Message);
}

public sealed record NotFoundError(string Code, string Message) : Error(Code, Message);

public sealed record ValidationError(string Code, string Message) : Error(Code, Message);

public sealed record ConflictError(string Code, string Message) : Error(Code, Message);

public sealed record UnauthorizedError(string Code, string Message) : Error(Code, Message);
// ...
