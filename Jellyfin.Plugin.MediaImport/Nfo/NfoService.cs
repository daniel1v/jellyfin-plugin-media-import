using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Jellyfin.Plugin.MediaImport.Services;

namespace Jellyfin.Plugin.MediaImport.Nfo;

/// <summary>
/// Implements Jellyfin's standard movie, series, and episode NFO sidecars.
/// </summary>
public sealed class NfoService : INfoService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    /// <inheritdoc />
    public IReadOnlyList<NfoDocument> CreateMovieDocuments(
        string mediaRelativePath,
        string title,
        int? year,
        string tmdbId)
    {
        var movieDirectory = GetMediaDirectory(mediaRelativePath);
        var normalizedTmdbId = ValidateTmdbId(tmdbId);
        var elements = new List<object>
        {
            new XElement("title", title)
        };
        AddYear(elements, year);
        elements.Add(new XElement("tmdbid", normalizedTmdbId));

        return
        [
            CreateDocument(Path.Combine(movieDirectory, "movie.nfo"), "movie", normalizedTmdbId, elements)
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<NfoDocument> CreateEpisodeDocuments(
        string mediaRelativePath,
        string seriesTitle,
        int? seriesYear,
        string seriesTmdbId,
        int seasonNumber,
        int episodeNumber,
        string episodeTitle,
        string? episodeTmdbId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seasonNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(episodeNumber);

        var seasonDirectory = GetMediaDirectory(mediaRelativePath);
        var seriesDirectory = Path.GetDirectoryName(seasonDirectory);
        if (string.IsNullOrWhiteSpace(seriesDirectory))
        {
            throw new ImportValidationException("The generated series path is invalid.");
        }

        var normalizedSeriesTmdbId = ValidateTmdbId(seriesTmdbId);
        var normalizedEpisodeTmdbId = string.IsNullOrWhiteSpace(episodeTmdbId)
            ? null
            : ValidateTmdbId(episodeTmdbId);
        var seriesElements = new List<object>
        {
            new XElement("title", seriesTitle)
        };
        AddYear(seriesElements, seriesYear);
        seriesElements.Add(new XElement("tmdbid", normalizedSeriesTmdbId));

        var episodeElements = new List<object>
        {
            new XElement("title", episodeTitle),
            new XElement("showtitle", seriesTitle),
            new XElement("season", seasonNumber.ToString(CultureInfo.InvariantCulture)),
            new XElement("episode", episodeNumber.ToString(CultureInfo.InvariantCulture))
        };
        if (normalizedEpisodeTmdbId is not null)
        {
            episodeElements.Add(new XElement("tmdbid", normalizedEpisodeTmdbId));
        }

        return
        [
            CreateDocument(Path.Combine(seriesDirectory, "tvshow.nfo"), "tvshow", normalizedSeriesTmdbId, seriesElements),
            CreateDocument(
                Path.ChangeExtension(mediaRelativePath, ".nfo"),
                "episodedetails",
                normalizedEpisodeTmdbId,
                episodeElements,
                seasonNumber,
                episodeNumber)
        ];
    }

    /// <inheritdoc />
    public void ValidateExisting(string path, NfoDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        if (Directory.Exists(path))
        {
            throw new ImportConflictException($"The NFO target '{document.RelativePath}' is a directory and cannot be reused.");
        }

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new ImportConflictException($"The existing NFO target '{document.RelativePath}' is a linked file and cannot be reused.");
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                MaxCharactersInDocument = 1_000_000,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(stream, settings);
            var existing = XDocument.Load(reader, LoadOptions.None);
            var root = existing.Root;
            if (root is null || !root.Name.LocalName.Equals(document.RootElementName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ImportConflictException($"The existing NFO target '{document.RelativePath}' has an unexpected document type.");
            }

            if (document.TmdbId is not null)
            {
                var providerIds = root
                    .DescendantsAndSelf()
                    .Where(IsTmdbIdElement)
                    .Select(element => element.Value.Trim());
                if (!providerIds.Contains(document.TmdbId, StringComparer.Ordinal))
                {
                    throw new ImportConflictException($"The existing NFO target '{document.RelativePath}' belongs to a different or unknown TMDb item.");
                }
            }

            if ((document.SeasonNumber.HasValue
                    && !HasIndexValue(root, "season", document.SeasonNumber.Value))
                || (document.EpisodeNumber.HasValue
                    && !HasIndexValue(root, "episode", document.EpisodeNumber.Value)))
            {
                throw new ImportConflictException($"The existing NFO target '{document.RelativePath}' belongs to a different episode.");
            }
        }
        catch (ImportConflictException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            throw new ImportConflictException($"The existing NFO target '{document.RelativePath}' could not be validated.", exception);
        }
    }

    /// <inheritdoc />
    public void WriteNew(string path, NfoDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, Utf8WithoutBom);
        writer.Write(document.Content);
    }

    private static NfoDocument CreateDocument(
        string relativePath,
        string rootElementName,
        string? tmdbId,
        IEnumerable<object> elements,
        int? seasonNumber = null,
        int? episodeNumber = null)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(rootElementName, elements));
        using var writer = new Utf8StringWriter(CultureInfo.InvariantCulture);
        using (var xmlWriter = XmlWriter.Create(
                   writer,
                   new XmlWriterSettings
                   {
                       Encoding = Utf8WithoutBom,
                       Indent = true,
                       NewLineChars = Environment.NewLine,
                       OmitXmlDeclaration = false
                   }))
        {
            xml.Save(xmlWriter);
        }

        return new NfoDocument(
            relativePath,
            rootElementName,
            tmdbId,
            writer.ToString() + Environment.NewLine,
            seasonNumber,
            episodeNumber);
    }

    private static string GetMediaDirectory(string mediaRelativePath)
    {
        if (string.IsNullOrWhiteSpace(mediaRelativePath) || Path.IsPathRooted(mediaRelativePath))
        {
            throw new ImportValidationException("The generated media path is invalid.");
        }

        var directory = Path.GetDirectoryName(mediaRelativePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ImportValidationException("The generated media path has no item directory.");
        }

        return directory;
    }

    private static string ValidateTmdbId(string tmdbId)
    {
        if (!int.TryParse(tmdbId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedId) || parsedId <= 0)
        {
            throw new ImportValidationException("The resolved TMDb provider ID is invalid.");
        }

        return parsedId.ToString(CultureInfo.InvariantCulture);
    }

    private static void AddYear(List<object> elements, int? year)
    {
        if (year.HasValue)
        {
            elements.Add(new XElement("year", year.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static bool IsTmdbIdElement(XElement element)
        => element.Name.LocalName.Equals("tmdbid", StringComparison.OrdinalIgnoreCase)
            || (element.Name.LocalName.Equals("uniqueid", StringComparison.OrdinalIgnoreCase)
                && element.Attribute("type")?.Value.Equals("tmdb", StringComparison.OrdinalIgnoreCase) == true);

    private static bool HasIndexValue(XElement root, string elementName, int expectedValue)
        => root.Elements()
            .Where(element => element.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Value.Trim())
            .Any(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                && parsedValue == expectedValue);

    private sealed class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(IFormatProvider formatProvider)
            : base(formatProvider)
        {
        }

        public override Encoding Encoding => Utf8WithoutBom;
    }
}
