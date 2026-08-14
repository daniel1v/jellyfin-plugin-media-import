using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.MediaImport.Services;

namespace Jellyfin.Plugin.MediaImport.Security;

/// <summary>
/// Prevents path traversal, unsupported sources, and link-based path escapes.
/// </summary>
public sealed class PathGuard : IPathGuard
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv",
        ".mp4",
        ".m4v"
    };

    /// <inheritdoc />
    public string ResolveSourcePath(string inboxRoot, string sourceFileName)
    {
        var fullRoot = ValidateExistingRoot(inboxRoot, "inbox");
        if (string.IsNullOrWhiteSpace(sourceFileName)
            || Path.IsPathRooted(sourceFileName)
            || !string.Equals(Path.GetFileName(sourceFileName), sourceFileName, StringComparison.Ordinal))
        {
            throw new ImportValidationException("The selected inbox filename is invalid.");
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(sourceFileName)))
        {
            throw new ImportValidationException("The source file has an unsupported video extension.");
        }

        var sourcePath = Path.GetFullPath(Path.Combine(fullRoot, sourceFileName));
        EnsureBelowRoot(fullRoot, sourcePath);
        if (!File.Exists(sourcePath))
        {
            throw new ImportConflictException("The source file is no longer available in the Media Import inbox.");
        }

        var attributes = File.GetAttributes(sourcePath);
        if (attributes.HasFlag(FileAttributes.Directory) || attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new ImportValidationException("Linked or non-regular inbox files cannot be imported.");
        }

        return sourcePath;
    }

    /// <inheritdoc />
    public string ResolveTargetPath(string libraryRoot, string relativeTargetPath)
    {
        var fullRoot = ValidateExistingRoot(libraryRoot, "library");
        if (string.IsNullOrWhiteSpace(relativeTargetPath) || Path.IsPathRooted(relativeTargetPath))
        {
            throw new ImportValidationException("The generated target path is invalid.");
        }

        var targetPath = Path.GetFullPath(Path.Combine(fullRoot, relativeTargetPath));
        EnsureBelowRoot(fullRoot, targetPath);
        EnsureExistingParentsAreNotLinks(fullRoot, Path.GetDirectoryName(targetPath)!);
        return targetPath;
    }

    private static string ValidateExistingRoot(string root, string description)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ImportValidationException($"The Media Import {description} path is not configured.");
        }

        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
        {
            throw new ImportValidationException($"The configured Media Import {description} path is unavailable.");
        }

        if (File.GetAttributes(fullRoot).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new ImportValidationException($"The configured Media Import {description} path cannot be a symbolic link.");
        }

        return fullRoot;
    }

    private static void EnsureBelowRoot(string fullRoot, string candidatePath)
    {
        var relative = Path.GetRelativePath(fullRoot, candidatePath);
        if (Path.IsPathRooted(relative)
            || relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ImportValidationException("The generated path leaves its configured Media Import root.");
        }
    }

    private static void EnsureExistingParentsAreNotLinks(string fullRoot, string targetDirectory)
    {
        var relativeDirectory = Path.GetRelativePath(fullRoot, targetDirectory);
        var current = fullRoot;
        foreach (var segment in relativeDirectory.Split(
                     new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new ImportValidationException("The target path contains a symbolic link.");
            }
        }
    }
}
