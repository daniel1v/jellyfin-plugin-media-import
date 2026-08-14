namespace Jellyfin.Plugin.MediaImport.Security;

/// <summary>
/// Resolves and validates all filesystem paths used by an import.
/// </summary>
public interface IPathGuard
{
    /// <summary>
    /// Resolves an inbox filename and rejects path escapes, unsupported files, and links.
    /// </summary>
    /// <param name="inboxRoot">The configured inbox root.</param>
    /// <param name="sourceFileName">The client-provided filename relative to the inbox.</param>
    /// <returns>The validated absolute source path.</returns>
    string ResolveSourcePath(string inboxRoot, string sourceFileName);

    /// <summary>
    /// Resolves a server-generated path below a configured library root.
    /// </summary>
    /// <param name="libraryRoot">The configured library root.</param>
    /// <param name="relativeTargetPath">The server-generated relative target path.</param>
    /// <returns>The validated absolute target path.</returns>
    string ResolveTargetPath(string libraryRoot, string relativeTargetPath);
}
