using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Parsing;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Reads the import queue represented by the configured inbox directory.
/// </summary>
public sealed class InboxService : IInboxService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv",
        ".mp4",
        ".m4v"
    };

    private readonly IFilenameParser _filenameParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxService"/> class.
    /// </summary>
    /// <param name="filenameParser">The conservative filename parser.</param>
    public InboxService(IFilenameParser filenameParser)
    {
        _filenameParser = filenameParser;
    }

    /// <inheritdoc />
    public IReadOnlyList<InboxFile> GetFiles(string inboxPath)
    {
        if (string.IsNullOrWhiteSpace(inboxPath))
        {
            throw new InvalidOperationException("The Media Import inbox is not configured.");
        }

        var fullInboxPath = Path.GetFullPath(inboxPath);
        if (!Directory.Exists(fullInboxPath))
        {
            throw new InvalidOperationException("The configured Media Import inbox is unavailable.");
        }

        return Directory
            .EnumerateFiles(fullInboxPath, "*", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(IsImportableRegularFile)
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToInboxFile)
            .ToArray();
    }

    private static bool IsImportableRegularFile(FileInfo file)
        => AllowedExtensions.Contains(file.Extension)
            && !file.Attributes.HasFlag(FileAttributes.Directory)
            && !file.Attributes.HasFlag(FileAttributes.ReparsePoint);

    private InboxFile ToInboxFile(FileInfo file)
        => new(
            file.Name,
            file.Extension.ToLowerInvariant(),
            file.Length,
            file.LastWriteTimeUtc,
            _filenameParser.Parse(file.Name));
}
