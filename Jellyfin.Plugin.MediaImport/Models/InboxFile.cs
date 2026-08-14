using System;

namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// A regular video file waiting in the configured inbox.
/// </summary>
public sealed record InboxFile(
    string FileName,
    string Extension,
    long SizeBytes,
    DateTime LastWriteTimeUtc,
    ParsedFileName ParsedFileName);
