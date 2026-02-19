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
            newResult.WithReasons(withResult.Reasons);

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
