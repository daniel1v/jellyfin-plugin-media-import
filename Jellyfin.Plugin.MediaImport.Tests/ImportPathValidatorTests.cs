using System;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class ImportPathValidatorTests
{
    private readonly ImportPathValidator _validator = new();

    [Fact]
    public void Accepts_separate_sibling_roots()
    {
        var root = CreateRoot();

        _validator.Validate(
            Path.Combine(root, "convert"),
            Path.Combine(root, "movies"),
            Path.Combine(root, "series"));
    }

    [Theory]
    [InlineData("convert", "convert", "series")]
    [InlineData("convert", "convert/movies", "series")]
    [InlineData("convert/movies", "convert", "series")]
    [InlineData("convert", "library", "library/series")]
    public void Rejects_identical_or_nested_roots(string inbox, string movies, string series)
    {
        var root = CreateRoot();

        Assert.Throws<ImportValidationException>(
            () => _validator.Validate(
                CombinePortable(root, inbox),
                CombinePortable(root, movies),
                CombinePortable(root, series)));
    }

    [Fact]
    public void Allows_unconfigured_optional_roots()
    {
        _validator.Validate(string.Empty, string.Empty, string.Empty);
    }

    private static string CreateRoot()
        => Path.Combine(Path.GetTempPath(), $"media-import-root-validation-{Guid.NewGuid():N}");

    private static string CombinePortable(string root, string relativePath)
        => relativePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(root, Path.Combine);
}
