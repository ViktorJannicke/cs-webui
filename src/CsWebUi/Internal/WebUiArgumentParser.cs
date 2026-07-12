using System.Globalization;

namespace CsWebUi.Internal;

internal static class WebUiArgumentParser
{
    internal static long ParseInt64(string value)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException($"WebUI argument '{value}' is not a JavaScript-compatible 64-bit integer.");
    }

    internal static double ParseDouble(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new FormatException($"WebUI argument '{value}' is not a JavaScript-compatible floating-point value.");
    }

    internal static bool ParseBoolean(string value)
    {
        return value switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(value, out var result) => result,
            _ => throw new FormatException($"WebUI argument '{value}' is not a JavaScript-compatible Boolean value."),
        };
    }
}
