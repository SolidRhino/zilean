namespace Zilean.Shared.Features.Python;

public static class CategoryClassifier
{
    private static readonly string[] _bookExtensions = [".epub", ".mobi", ".azw3", ".cbr", ".cbz"];
    private static readonly string[] _bookKeywords = ["ebook", "epub", "azw3", "cbz"];
    private static readonly string[] _pdfBookKeywords = ["ebook", "epub", "textbook", "manga"];
    private static readonly string[] _audiobookKeywords = ["audiobook", "narrated by", "unabridged", "abridged"];

    public static string DetectCategory(string? extension, string? rawTitle, bool isAdult, string mediaType)
    {
        if (isAdult)
        {
            return "xxx";
        }

        var title = (rawTitle ?? string.Empty).Replace('.', ' ');
        var ext = extension?.ToLowerInvariant() ?? string.Empty;

        // Audiobook detection (before book - more specific)
        if (ext == ".m4b")
        {
            return "audiobook";
        }

        if (_audiobookKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return "audiobook";
        }

        if (ext == ".mp3")
        {
            // mp3 without audiobook keyword falls through to movie/TV
            return mediaType.Equals("movie", StringComparison.OrdinalIgnoreCase) ? "movie" : "tvSeries";
        }

        // Book detection
        if (_bookExtensions.Contains(ext))
        {
            return "book";
        }

        if (ext == ".pdf" && _pdfBookKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return "book";
        }

        if (string.IsNullOrEmpty(ext) && _bookKeywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            return "book";
        }

        // Fallback to movie/TV
        return mediaType.Equals("movie", StringComparison.OrdinalIgnoreCase) ? "movie" : "tvSeries";
    }
}