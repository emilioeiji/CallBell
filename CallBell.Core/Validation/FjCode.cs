using System.Text.RegularExpressions;

namespace CallBell.Core.Validation;

public static partial class FjCode
{
    public static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    public static bool IsValid(string? value)
    {
        return FjPattern().IsMatch(Normalize(value));
    }

    public static void EnsureValid(string? value, string fieldName = "FJ code")
    {
        if (!IsValid(value))
        {
            throw new InvalidOperationException($"{fieldName} deve estar no formato FJ12345.");
        }
    }

    [GeneratedRegex("^FJ\\d{5}$", RegexOptions.Compiled)]
    private static partial Regex FjPattern();
}
