using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MyNetatmo24.SharedKernel.Infrastructure;

public static class HelperExtensions
{
    extension<T>([NotNull] T? obj)
    {
        public T ThrowIfNull([CallerArgumentExpression(nameof(obj))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(obj, paramName);
            return obj;
        }
    }

    extension([NotNull] string? value)
    {
        public string ThrowIfNullOrEmpty([CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(value, paramName);
            return value;
        }

        public string ThrowIfNullOrWhiteSpace([CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
            return value;
        }
    }
}
