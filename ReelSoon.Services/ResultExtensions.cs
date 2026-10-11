namespace ReelSoon.Services;

/// <summary>
/// Converts a failed result between value types while preserving why it failed.
/// </summary>
public static class ResultExtensions
{
    public static bool IsSuccessStatus(this IResult result) =>
        result.Status is ResultStatus.Ok or ResultStatus.Created or ResultStatus.NoContent;

    public static Result ToVoid(this IResult result)
    {
        if (result.IsSuccessStatus())
        {
            return Result.Success();
        }

        var errors = result.Errors?.ToArray() ?? [];

        return result.Status switch
        {
            ResultStatus.NotFound => Result.NotFound(errors),
            ResultStatus.Forbidden => Result.Forbidden(errors),
            ResultStatus.Unauthorized => Result.Unauthorized(errors),
            ResultStatus.Invalid => Result.Invalid([.. result.ValidationErrors ?? []]),
            ResultStatus.Conflict => Result.Conflict(errors),
            ResultStatus.Unavailable => Result.Unavailable(errors),
            ResultStatus.CriticalError => Result.CriticalError(errors),
            _ => Result.Error(new ErrorList(errors)),
        };
    }

    public static Result<TTo> To<TTo>(this IResult result)
    {
        if (result.IsSuccessStatus())
        {
            throw new InvalidOperationException(
                $"{nameof(To)} propagates failures; a successful result cannot be converted to " +
                $"Result<{typeof(TTo).Name}> without a projection.");
        }

        var errors = result.Errors?.ToArray() ?? [];

        return result.Status switch
        {
            ResultStatus.NotFound => Result<TTo>.NotFound(errors),
            ResultStatus.Forbidden => Result<TTo>.Forbidden(errors),
            ResultStatus.Unauthorized => Result<TTo>.Unauthorized(errors),
            ResultStatus.Invalid => Result<TTo>.Invalid([.. result.ValidationErrors ?? []]),
            ResultStatus.Conflict => Result<TTo>.Conflict(errors),
            ResultStatus.Unavailable => Result<TTo>.Unavailable(errors),
            ResultStatus.CriticalError => Result<TTo>.CriticalError(errors),
            _ => Result<TTo>.Error(new ErrorList(errors)),
        };
    }

    public static string GetDisplayMessage(this IResult result)
    {
        string? first = result.Errors?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first))
        {
            return first;
        }

        var validation = result.ValidationErrors?.FirstOrDefault();
        if (validation is not null && !string.IsNullOrWhiteSpace(validation.ErrorMessage))
        {
            return validation.ErrorMessage;
        }

        return result.Status switch
        {
            ResultStatus.NotFound => "That item no longer exists.",
            ResultStatus.Forbidden => "You do not have permission to do that.",
            ResultStatus.Unauthorized => "Please sign in and try again.",
            ResultStatus.Conflict => "That conflicts with something that already exists.",
            ResultStatus.Invalid => "Some of the values supplied were not valid.",
            _ => "Something went wrong. Please try again.",
        };
    }
}
