using System.Collections.Generic;
using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Lists importable files from a configured handoff directory.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Lists supported regular video files directly inside an inbox directory.
    /// </summary>
    /// <param name="inboxPath">The configured inbox directory.</param>
    /// <returns>The supported files, without exposing the absolute inbox path.</returns>
    IReadOnlyList<InboxFile> GetFiles(string inboxPath);
}
