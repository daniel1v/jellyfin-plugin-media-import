using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MediaImport.Services;

namespace Jellyfin.Plugin.MediaImport.Naming;

/// <summary>
/// Creates deterministic, portable Jellyfin media names.
/// </summary>
public sealed partial class NamingService : INamingService
{
    private const string InvalidPortableCharacters = "<>:\"/\\|?*";

    /// <inheritdoc />
    public string GetMovieRelativePath(string title, int? year, string extension)
    {
        var safeTitle = SanitizeSegment(title);
        var safeExtension = ValidateExtension(extension);
        var folderName = string.Create(CultureInfo.InvariantCulture, $"{safeTitle}{FormatYear(year)}");
        return Path.Combine(folderName, folderName + safeExtension);
    }

    /// <inheritdoc />
    public string GetEpisodeRelativePath(
        string seriesTitle,
        int? seriesYear,
        int seasonNumber,
        int episodeNumber,
        string episodeTitle,
        string extension)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seasonNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(episodeNumber);

        var safeSeriesTitle = SanitizeSegment(seriesTitle);
        var safeEpisodeTitle = SanitizeSegment(episodeTitle);
        var seriesFolder = string.Create(CultureInfo.InvariantCulture, $"{safeSeriesTitle}{FormatYear(seriesYear)}");
        var seasonFolder = string.Create(CultureInfo.InvariantCulture, $"Season {seasonNumber:00}");
        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"{safeSeriesTitle} S{seasonNumber:00}E{episodeNumber:00} {safeEpisodeTitle}{ValidateExtension(extension)}");
        return Path.Combine(seriesFolder, seasonFolder, fileName);
    }

    private static string SanitizeSegment(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            builder.Append(character < ' ' || InvalidPortableCharacters.Contains(character, StringComparison.Ordinal) ? ' ' : character);
        }

        var sanitized = WhitespaceRegex().Replace(builder.ToString(), " ").Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ImportValidationException("The resolved title cannot be converted into a safe filename.");
        }

        return sanitized;
    }

    private static string ValidateExtension(string extension)
    {
        if (extension is not ".mkv" and not ".mp4" and not ".m4v")
        {
            throw new ImportValidationException("The source file has an unsupported video extension.");
        }

        return extension;
    }

    private static string FormatYear(int? year)
        => year.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $" ({year.Value:0000})")
            : string.Empty;

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
