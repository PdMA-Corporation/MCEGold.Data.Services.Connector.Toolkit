namespace RuntimeSmokeValidation.Diagnostics;

internal static class Sanitizer
{
    public static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
    }

    public static string Configured(string value) => string.IsNullOrWhiteSpace(value) ? "missing" : "configured";
}
