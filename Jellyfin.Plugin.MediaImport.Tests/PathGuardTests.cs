using System;
using System.IO;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class PathGuardTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"media-import-path-tests-{Guid.NewGuid():N}");
    private readonly PathGuard _guard = new();

    public PathGuardTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void Resolves_a_regular_supported_inbox_file()
    {
        var source = Path.Combine(_root, "movie.mkv");
        File.WriteAllBytes(source, new byte[] { 1 });

        var resolved = _guard.ResolveSourcePath(_root, "movie.mkv");

        Assert.Equal(source, resolved);
    }

    [Fact]
    public void Rejects_source_path_traversal()
    {
        Assert.Throws<ImportValidationException>(
            () => _guard.ResolveSourcePath(_root, Path.Combine("..", "outside.mkv")));
    }

    [Fact]
    public void Rejects_generated_target_path_traversal()
    {
        Assert.Throws<ImportValidationException>(
            () => _guard.ResolveTargetPath(_root, Path.Combine("..", "outside.mkv")));
    }

    [Fact]
    public void Rejects_unsupported_source_extension()
    {
        File.WriteAllBytes(Path.Combine(_root, "notes.txt"), new byte[] { 1 });

        Assert.Throws<ImportValidationException>(() => _guard.ResolveSourcePath(_root, "notes.txt"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}
