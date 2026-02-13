using Vogen;

namespace MyNetatmo24.SharedKernel.StronglyTypedIds;

[ValueObject<Guid>(Conversions.SystemTextJson | Conversions.EfCoreValueConverter)]
public readonly partial struct AccountId
{
    public static AccountId New() => From(Guid.CreateVersion7());

    private static Guid NormalizeInput(Guid input) => input;

    private static Validation Validate(Guid value)
    {
        var isValid = value != Guid.Empty;
        return isValid ? Validation.Ok : Validation.Invalid("UserId must not be empty");
    }
}
