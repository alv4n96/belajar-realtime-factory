namespace EQFR.Common;

public readonly record struct Result(bool IsSuccess, ErrorDetail? Error)
{
    public static Result Success() => new(true, null);
    public static Result Failure(string code, string message) => new(false, new ErrorDetail(code, message));
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, ErrorDetail? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string message) => new(false, default, new ErrorDetail(code, message));
}

