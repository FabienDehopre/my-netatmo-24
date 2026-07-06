using FluentResults;
using Microsoft.AspNetCore.Http;

namespace MyNetatmo24.SharedKernel.Results;

#pragma warning disable CA1708
public static class ResultExtensions
#pragma warning restore CA1708
{
    // extension(Result result)
    // {
    //
    // }

    extension(Task<Result> resultTask)
    {
        public async Task<Result> Compensate(Func<Result, Result> compensator)
        {
            ArgumentNullException.ThrowIfNull(compensator);

            var result = await resultTask;
            if (result.IsSuccess)
            {
                return result;
            }

            return compensator(result);
        }
    }

    // extension<T>(Result<T> result)
    // {
    //
    // }

    extension<T>(Task<Result<T>> resultTask)
    {
        public async Task<Result<TResult>> BindWith<TWith, TResult>(Result<TWith> withResult, Func<T, TWith, Result<TResult>> bind)
        {
            ArgumentNullException.ThrowIfNull(withResult);
            ArgumentNullException.ThrowIfNull(bind);

            var result = await resultTask;
            var newResult = new Result<TResult>();
            newResult.WithReasons(result.Reasons);
            // Only carry withResult's reasons that the chain doesn't already hold. When withResult is
            // the failed head of this same chain, its error has already propagated into result.Reasons;
            // adding it again would double-count it (e.g. a single 401 would appear twice).
            newResult.WithReasons(withResult.Reasons.Where(reason => !result.Reasons.Contains(reason)));

            if (result.IsSuccess && withResult.IsSuccess)
            {
                var bindResult = bind(result.Value, withResult.Value);
                newResult.WithReasons(bindResult.Reasons);
                newResult.WithValue(bindResult.Value);
            }

            return newResult;
        }
    }
}
