namespace MyNetatmo24.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failure result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result<TValue> Success<TValue>(TValue value) => new(value);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Failure<TValue>(Error error) => new(error);

    // Implicit conversions for cleaner syntax
#pragma warning disable CA2225
    public static implicit operator Result(Error error) => Failure(error);
#pragma warning restore CA2225
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue value) : base(true, Error.None) => _value = value;

    protected internal  Result(Error error) : base(false, error) => _value = default;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    // Implicit conversions for cleaner syntax
#pragma warning disable CA2225
    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
#pragma warning restore CA2225
}
