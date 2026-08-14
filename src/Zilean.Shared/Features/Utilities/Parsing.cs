using System.Security.Cryptography;

namespace Zilean.Shared.Features.Utilities;

/// <summary>
/// Provides static utility methods for parsing and normalizing torrent names,
/// IMDb identifiers, byte-size strings, and query strings.
/// </summary>
public static partial class Parsing
{
    [GeneratedRegex(
        @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
        RegexOptions.Compiled)]
    private static partial Regex InvalidXmlChars();

    [GeneratedRegex(@"^(?:tt)?(\d{1,8})$", RegexOptions.Compiled)]
    private static partial Regex ImdbIdRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();
    [GeneratedRegex(@"(?i)\b(?:a|the|and|of|in|on|with|to|for|by|is|it)\b", RegexOptions.Compiled)]
    private static partial Regex StopWordRegex();
    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex SpaceRemovalRegex();
    [GeneratedRegex(@"\s+\(?(19\d{2}|20\d{2})\)?\s*$", RegexOptions.Compiled)]
    private static partial Regex TrailingYearRegex();

    /// <summary>
    /// Trims leading and trailing whitespace from the given string.
    /// </summary>
    /// <param name="s">The string to normalize, or <c>null</c>.</param>
    /// <returns>The trimmed string, or <see cref="string.Empty"/> if <paramref name="s"/> is <c>null</c>.</returns>
    public static string NormalizeSpace(string s) => s?.Trim() ?? string.Empty;

    /// <summary>
    /// Trims the string and collapses consecutive whitespace into single spaces.
    /// </summary>
    /// <param name="s">The string to normalize.</param>
    /// <returns>The string with all runs of whitespace replaced by single spaces.</returns>
    public static string NormalizeMultiSpaces(string s) =>
        SpaceRegex().Replace(NormalizeSpace(s), " ");
    private static string NormalizeNumber(string s, bool isInt = false)
    {
        var valStr = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

        valStr = valStr.Trim().Replace("-", "0");

        if (isInt)
        {
            if (valStr.Contains(',') && valStr.Contains('.'))
            {
                return valStr;
            }

            valStr = valStr.Length == 0 ? "0" : valStr.Replace(".", ",");

            return valStr;
        }

        valStr = valStr.Length == 0 ? "0" : valStr.Replace(",", ".");

        if (valStr.Count(c => c == '.') > 1)
        {
            var lastOcc = valStr.LastIndexOf('.');
            valStr = valStr[..lastOcc].Replace(".", string.Empty) + valStr[lastOcc..];
        }

        return valStr;
    }

    /// <summary>
    /// Builds a <c>magnet:</c> URI from the given infohash.
    /// </summary>
    /// <param name="infohash">The BitTorrent infohash (SHA1 hex string), or <c>null</c>.</param>
    /// <returns>A <see cref="Uri"/> for the magnet link, or <c>null</c> if <paramref name="infohash"/> is blank.</returns>
    public static Uri? GetMagnetLink(string? infohash) =>
        string.IsNullOrWhiteSpace(infohash) ? null : new Uri($"magnet:?xt=urn:btih:{infohash}");

    /// <summary>
    /// Creates a deterministic <see cref="Guid"/> by hashing the given infohash with SHA-256.
    /// </summary>
    /// <param name="infohash">A 40-character hexadecimal infohash string.</param>
    /// <returns>A <see cref="Guid"/> derived from the first 16 bytes of the SHA-256 hash.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="infohash"/> is null or not exactly 40 characters.</exception>
    public static Guid CreateGuidFromInfohash(string? infohash)
    {
        if (string.IsNullOrEmpty(infohash) || infohash.Length != 40)
        {
            throw new ArgumentException("Infohash must be a 40-character hexadecimal string.", nameof(infohash));
        }

        using var hasher = SHA256.Create();
        byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(infohash));
        return new Guid(new Span<byte>(hash, 0, 16));
    }


    /// <summary>
    /// Removes XML-invalid characters (unpaired surrogates, control chars, BOM) from the given string.
    /// </summary>
    /// <param name="text">The input string to sanitize.</param>
    /// <returns>The string with all XML-invalid characters removed, or <c>""</c> if input is null or empty.</returns>
    public static string RemoveInvalidXmlChars(string text) =>
        string.IsNullOrEmpty(text) ? "" : InvalidXmlChars().Replace(text, "");

    /// <summary>
    /// Parses a numeric string into a <see cref="double"/> using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse (may contain commas/dots as separators).</param>
    /// <returns>The parsed <see cref="double"/> value.</returns>
    public static double CoerceDouble(string str) =>
        double.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a numeric string into a <see cref="float"/> using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse (may contain commas/dots as separators).</param>
    /// <returns>The parsed <see cref="float"/> value.</returns>
    public static float CoerceFloat(string str) =>
        float.Parse(NormalizeNumber(str), NumberStyles.Any, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a numeric string into a 32-bit integer using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <returns>The parsed <see cref="int"/> value.</returns>
    public static int CoerceInt(string str) =>
        int.Parse(NormalizeNumber(str, true), NumberStyles.Any, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a numeric string into a 64-bit integer using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <returns>The parsed <see cref="long"/> value.</returns>
    public static long CoerceLong(string str) =>
        long.Parse(NormalizeNumber(str, true), NumberStyles.Any, CultureInfo.InvariantCulture);

    /// <summary>
    /// Attempts to parse a numeric string into a <see cref="double"/> using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful, or zero otherwise.</param>
    /// <returns><c>true</c> if the string was successfully parsed; otherwise <c>false</c>.</returns>
    public static bool TryCoerceDouble(string str, out double result) => double.TryParse(NormalizeNumber(str), NumberStyles.Any,
        CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Attempts to parse a numeric string into a <see cref="float"/> using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful, or zero otherwise.</param>
    /// <returns><c>true</c> if the string was successfully parsed; otherwise <c>false</c>.</returns>
    public static bool TryCoerceFloat(string str, out float result) => float.TryParse(NormalizeNumber(str), NumberStyles.Any,
        CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Attempts to parse a numeric string into a 32-bit integer using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful, or zero otherwise.</param>
    /// <returns><c>true</c> if the string was successfully parsed; otherwise <c>false</c>.</returns>
    public static bool TryCoerceInt(string str, out int result) => int.TryParse(NormalizeNumber(str, true), NumberStyles.Any,
        CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Attempts to parse a numeric string into a 64-bit integer using invariant culture.
    /// </summary>
    /// <param name="str">The numeric string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful, or zero otherwise.</param>
    /// <returns><c>true</c> if the string was successfully parsed; otherwise <c>false</c>.</returns>
    public static bool TryCoerceLong(string str, out long result) => long.TryParse(NormalizeNumber(str, true), NumberStyles.Any,
        CultureInfo.InvariantCulture, out result);

    /// <summary>
    /// Extracts a query-string argument value from a URL.
    /// </summary>
    /// <param name="url">The URL containing a query string, or <c>null</c>.</param>
    /// <param name="argument">The query-string parameter name to extract, or <c>null</c>.</param>
    /// <returns>The first value for the given argument, or <c>null</c> if not found.</returns>
    public static string GetArgumentFromQueryString(string? url, string? argument)
    {
        if (url == null || argument == null)
        {
            return null;
        }

        var qsStr = url.Split(['?'], 2)[1];
        qsStr = qsStr.Split(['#'], 2)[0];
        var qs = QueryHelpers.ParseQuery(qsStr);
        return qs[argument].FirstOrDefault();
    }

    /// <summary>
    /// Extracts the first contiguous sequence of digits from a string and parses it as a 64-bit integer.
    /// </summary>
    /// <param name="str">The string to extract a number from.</param>
    /// <returns>The extracted <see cref="long"/>, or <c>null</c> if no digits are found.</returns>
    public static long? GetLongFromString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        var extractedLong = string.Empty;

        foreach (var c in str)
        {
            if (c is < '0' or > '9')
            {
                if (extractedLong.Length > 0)
                {
                    break;
                }

                continue;
            }

            extractedLong += c;
        }

        return CoerceLong(extractedLong);
    }

    /// <summary>
    /// Extracts the numeric portion of an IMDb identifier from a string (with or without the <c>tt</c> prefix).
    /// </summary>
    /// <param name="value">The string containing an IMDb identifier, or <c>null</c>.</param>
    /// <returns>The numeric IMDb ID, or <c>null</c> if the string does not match the IMDb ID pattern.</returns>
    public static int? GetImdbId(string? value)
    {
        if (value == null)
        {
            return null;
        }

        var match = ImdbIdRegex().Match(value);

        return !match.Success ? null : int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns a fully-formatted IMDb identifier (<c>tt</c> prefix plus 7-digit zero-padded number).
    /// </summary>
    /// <param name="value">The string containing an IMDb identifier, or <c>null</c>.</param>
    /// <returns>The formatted <c>tt#######</c> identifier, or <c>null</c> if no valid ID was found.</returns>
    public static string GetFullImdbId(string? value)
    {
        var imdbId = GetImdbId(value);

        return imdbId is null or 0 ? null : $"tt{imdbId.GetValueOrDefault():D7}";
    }

    // ex: " 3.5  gb   " -> "3758096384" , "3,5GB" -> "3758096384" ,  "296,98 MB" -> "311406100.48" , "1.018,29 MB" -> "1067754455.04"
    // ex:  "1.018.29mb" -> "1067754455.04" , "-" -> "0" , "---" -> "0"
    /// <summary>
    /// Parses a human-readable size string (e.g. <c>"3.5 GB"</c>) into a byte count.
    /// </summary>
    /// <param name="str">The size string with a numeric value and optional unit suffix.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long GetBytes(string str)
    {
        var valStr = new string(str.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        valStr = (valStr.Length == 0) ? "0" : valStr.Replace(",", ".");
        if (valStr.Count(c => c == '.') > 1)
        {
            var lastOcc = valStr.LastIndexOf('.');
            valStr = valStr[..lastOcc].Replace(".", string.Empty) + valStr[lastOcc..];
        }

        var unit = new string(str.Where(char.IsLetter).ToArray());
        var val = CoerceFloat(valStr);
        return GetBytes(unit, val);
    }

    /// <summary>
    /// Converts a numeric value with a unit string (e.g. <c>"GB"</c>, <c>"MB"</c>) into a byte count.
    /// </summary>
    /// <param name="unit">The unit string (KB, MB, GB, TB; with or without an <c>i</c> suffix).</param>
    /// <param name="value">The numeric size in the given unit.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long GetBytes(string unit, float value)
    {
        unit = unit.Replace("i", "").ToLowerInvariant();

        return unit.Contains("kb")
            ? BytesFromKB(value)
            : unit.Contains("mb")
            ? BytesFromMB(value)
            : unit.Contains("gb") ? BytesFromGB(value) : unit.Contains("tb") ? BytesFromTB(value) : (long)value;
    }

    /// <summary>
    /// Converts terabytes to bytes.
    /// </summary>
    /// <param name="tb">The value in terabytes.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long BytesFromTB(float tb) => BytesFromGB(tb * 1024f);

    /// <summary>
    /// Converts gigabytes to bytes.
    /// </summary>
    /// <param name="gb">The value in gigabytes.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long BytesFromGB(float gb) => BytesFromMB(gb * 1024f);

    /// <summary>
    /// Converts megabytes to bytes.
    /// </summary>
    /// <param name="mb">The value in megabytes.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long BytesFromMB(float mb) => BytesFromKB(mb * 1024f);

    /// <summary>
    /// Converts kilobytes to bytes.
    /// </summary>
    /// <param name="kb">The value in kilobytes.</param>
    /// <returns>The equivalent size in bytes.</returns>
    public static long BytesFromKB(float kb) => (long)(kb * 1024f);

    /// <summary>
    /// Removes common English stop words and collapses extra whitespace from a search query.
    /// </summary>
    /// <param name="query">The raw query string.</param>
    /// <returns>The cleaned query with stop words removed and whitespace normalized.</returns>
    public static string CleanQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return query;
        }

        var cleanedQuery = StopWordRegex().Replace(query, "");
        cleanedQuery = SpaceRemovalRegex().Replace(cleanedQuery, " ").Trim();

        return cleanedQuery;
    }

    /// <summary>
    /// Extracts a trailing year (1900–2099) from the end of a query string.
    /// </summary>
    /// <param name="query">The query string potentially ending with a year in parentheses.</param>
    /// <returns>A tuple of the query with the year removed and the extracted year, or <c>null</c> if no year was found.</returns>
    public static (string? Query, int? Year) ExtractTrailingYear(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (query, null);
        }

        var match = TrailingYearRegex().Match(query);
        if (!match.Success)
        {
            return (query, null);
        }

        var year = int.Parse(match.Groups[1].Value);
        var cleaned = query[..match.Index].TrimEnd();
        return (cleaned, year);
    }
}