using System;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Parsing;

/// <summary>
/// Extracts only well-known title, year, season, and episode patterns.
/// </summary>
public sealed partial class FilenameParser : IFilenameParser
{
    /// <inheritdoc />
    public ParsedFileName Parse(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var stem = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (GenericRipNameRegex().IsMatch(stem))
        {
            return new ParsedFileName(null, null, null, null, true);
        }

        var episodeMatch = EpisodeRegex().Match(stem);
        if (!episodeMatch.Success)
        {
            episodeMatch = AlternateEpisodeRegex().Match(stem);
        }

        int? seasonNumber = null;
        int? episodeNumber = null;
        var titlePortion = stem;
        if (episodeMatch.Success)
        {
            seasonNumber = int.Parse(episodeMatch.Groups["season"].Value, System.Globalization.CultureInfo.InvariantCulture);
            episodeNumber = int.Parse(episodeMatch.Groups["episode"].Value, System.Globalization.CultureInfo.InvariantCulture);
            titlePortion = stem[..episodeMatch.Index];
        }

        var yearMatch = YearRegex().Match(titlePortion);
        int? year = null;
        if (yearMatch.Success)
        {
            year = int.Parse(yearMatch.Groups["year"].Value, System.Globalization.CultureInfo.InvariantCulture);
            titlePortion = titlePortion[..yearMatch.Groups["year"].Index];
        }

        var title = NormalizeTitle(titlePortion);
        return new ParsedFileName(
            string.IsNullOrWhiteSpace(title) ? null : title,
            year,
            seasonNumber,
            episodeNumber,
            false);
    }

    private static string NormalizeTitle(string value)
    {
        var separated = SeparatorsRegex().Replace(value, " ");
        return WhitespaceRegex().Replace(separated, " ").Trim(' ', '-', '(', ')', '[', ']');
    }

    [GeneratedRegex("^title_t\\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GenericRipNameRegex();

    [GeneratedRegex("(?:^|[ ._\\-])S(?<season>\\d{1,2})E(?<episode>\\d{1,3})(?:$|[ ._\\-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EpisodeRegex();

    [GeneratedRegex("(?:^|[ ._\\-])(?<season>\\d{1,2})x(?<episode>\\d{1,3})(?:$|[ ._\\-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlternateEpisodeRegex();

    [GeneratedRegex("(?:^|[ ._\\-(])(?<year>(?:19|20)\\d{2})(?:$|[ ._\\-)])", RegexOptions.CultureInvariant)]
    private static partial Regex YearRegex();

    [GeneratedRegex("[._]+", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorsRegex();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
