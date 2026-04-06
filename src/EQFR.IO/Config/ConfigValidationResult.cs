namespace EQFR.IO.Config;

public sealed record ConfigValidationResult(IReadOnlyList<ConfigValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static ConfigValidationResult Ok() => new(Array.Empty<ConfigValidationError>());
    public static ConfigValidationResult Fail(params ConfigValidationError[] errors) => new(errors);
}

