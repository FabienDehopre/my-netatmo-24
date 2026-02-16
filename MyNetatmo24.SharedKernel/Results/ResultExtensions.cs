// namespace MyNetatmo24.SharedKernel.Results;
//
// public static class ResultExtensions
// {
//     // Transform the value if successful
//     public static Result<TOut> Map<TIn, TOut>(
//         this Result<TIn> result,
//         Func<TIn, TOut> mapper)
//     {
//         ArgumentNullException.ThrowIfNull(result);
//         ArgumentNullException.ThrowIfNull(mapper);
//
//         return result.IsSuccess
//             ? Result.Success(mapper(result.Value))
//             : Result.Failure<TOut>(result.Error);
//     }
//
//     // Chain operations that return Results
//     public static Result<TOut> Bind<TIn, TOut>(
//         this Result<TIn> result,
//         Func<TIn, Result<TOut>> binder)
//     {
//         ArgumentNullException.ThrowIfNull(result);
//         ArgumentNullException.ThrowIfNull(binder);
//
//         return result.IsSuccess
//             ? binder(result.Value)
//             : Result.Failure<TOut>(result.Error);
//     }
//
//     // Handle both success and failure cases
//     public static TOut Match<TIn, TOut>(
//         this Result<TIn> result,
//         Func<TIn, TOut> onSuccess,
//         Func<Error, TOut> onFailure)
//     {
//         ArgumentNullException.ThrowIfNull(result);
//         ArgumentNullException.ThrowIfNull(onSuccess);
//         ArgumentNullException.ThrowIfNull(onFailure);
//
//         return result.IsSuccess
//             ? onSuccess(result.Value)
//             : onFailure(result.Error);
//     }
//
//     // Execute action on success
//     public static Result<T> Tap<T>(
//         this Result<T> result,
//         Action<T> action)
//     {
//         ArgumentNullException.ThrowIfNull(result);
//         ArgumentNullException.ThrowIfNull(action);
//
//         if (result.IsSuccess)
//         {
//             action(result.Value);
//         }
//
//         return result;
//     }
//
//     // Convert Result to Result<T>
//     public static Result<T> ToResult<T>(this Result result, T value)
//     {
//         ArgumentNullException.ThrowIfNull(result);
//
//         return result.IsSuccess
//             ? Result.Success(value)
//             : Result.Failure<T>(result.Error);
//     }
// }
