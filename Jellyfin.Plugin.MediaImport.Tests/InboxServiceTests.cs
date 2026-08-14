using System;
using System.IO;
using Jellyfin.Plugin.MediaImport.Parsing;
using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class InboxServiceTests : IDisposable
{
    private readonly string _inboxPath = Path.Combine(Path.GetTempPath(), $"media-import-tests-{Guid.NewGuid():N}");
    private readonly InboxService _service = new(new FilenameParser());

    public InboxServiceTests()
    {
        Directory.CreateDirectory(_inboxPath);
    }

    [Fact]
    public void Lists_only_supported_files_in_the_direct_inbox()
    {
        File.WriteAllBytes(Path.Combine(_inboxPath, "B.mkv"), new byte[] { 1, 2 });
        File.WriteAllBytes(Path.Combine(_inboxPath, "a.MP4"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_inboxPath, "notes.txt"), new byte[] { 1 });
        var nested = Directory.CreateDirectory(Path.Combine(_inboxPath, "nested"));
        File.WriteAllBytes(Path.Combine(nested.FullName, "hidden.m4v"), new byte[] { 1 });

        var files = _service.GetFiles(_inboxPath);

        Assert.Collection(
            files,
            file =>
            {
                Assert.Equal("a.MP4", file.FileName);
                Assert.Equal(".mp4", file.Extension);
                Assert.Equal(1, file.SizeBytes);
            },
            file => Assert.Equal("B.mkv", file.FileName));
    }

    [Fact]
    public void Does_not_expose_the_absolute_inbox_path()
    {
        File.WriteAllBytes(Path.Combine(_inboxPath, "Dune.2021.mkv"), new byte[] { 1 });

        var file = Assert.Single(_service.GetFiles(_inboxPath));

        Assert.Equal("Dune.2021.mkv", file.FileName);
        Assert.DoesNotContain(_inboxPath, file.FileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_an_unconfigured_inbox()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _service.GetFiles(string.Empty));

        Assert.Equal("The Media Import inbox is not configured.", exception.Message);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_inboxPath, true);
    }
}
