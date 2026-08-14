using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Configuration;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Naming;
using Jellyfin.Plugin.MediaImport.Nfo;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class ImportServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"media-import-service-tests-{Guid.NewGuid():N}");
    private readonly string _inboxPath;
    private readonly string _moviesPath;
    private readonly string _seriesPath;
    private readonly TestLibraryScanService _scanService = new();

    public ImportServiceTests()
    {
        _inboxPath = Directory.CreateDirectory(Path.Combine(_root, "inbox")).FullName;
        _moviesPath = Directory.CreateDirectory(Path.Combine(_root, "movies")).FullName;
        _seriesPath = Directory.CreateDirectory(Path.Combine(_root, "series")).FullName;
    }

    [Fact]
    public async Task Imports_movie_without_overwriting_and_queues_scan()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 1, 2, 3 });
        using var service = CreateService();

        var result = await service.ImportAsync(CreateMovieRequest(), CancellationToken.None);

        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(result.Plan.DestinationPath));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(result.Plan.DestinationPath));
        var movieNfoPath = Path.Combine(Path.GetDirectoryName(result.Plan.DestinationPath)!, "movie.nfo");
        Assert.True(File.Exists(movieNfoPath));
        Assert.Contains("<tmdbid>693134</tmdbid>", File.ReadAllText(movieNfoPath), StringComparison.Ordinal);
        Assert.True(result.LibraryScanQueued);
        Assert.Equal(1, _scanService.QueueCount);
    }

    [Fact]
    public async Task Existing_target_is_never_overwritten()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 1, 2, 3 });
        var namingService = new NamingService();
        var relativeTarget = namingService.GetMovieRelativePath("Dune: Part Two", 2024, ".mkv");
        var targetPath = Path.Combine(_moviesPath, relativeTarget);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllBytes(targetPath, new byte[] { 9, 9 });
        using var service = CreateService(namingService);

        await Assert.ThrowsAsync<ImportConflictException>(
            () => service.ImportAsync(CreateMovieRequest(), CancellationToken.None));

        Assert.True(File.Exists(sourcePath));
        Assert.Equal(new byte[] { 9, 9 }, File.ReadAllBytes(targetPath));
        Assert.Equal(0, _scanService.QueueCount);
    }

    [Fact]
    public async Task Scan_failure_does_not_invalidate_successful_file_move()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 4 });
        _scanService.ThrowOnQueue = true;
        using var service = CreateService();

        var result = await service.ImportAsync(CreateMovieRequest(), CancellationToken.None);

        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(result.Plan.DestinationPath));
        Assert.False(result.LibraryScanQueued);
    }

    [Fact]
    public async Task Series_preview_uses_fresh_series_and_episode_metadata()
    {
        File.WriteAllBytes(Path.Combine(_inboxPath, "Bluey.S02E14.mkv"), new byte[] { 1 });
        using var service = CreateService();
        var request = new ImportRequest
        {
            SourceFileName = "Bluey.S02E14.mkv",
            MediaType = ImportMediaType.Series,
            TmdbId = "82728",
            SeasonNumber = 2,
            EpisodeNumber = 14
        };

        var plan = await service.PreviewAsync(request, CancellationToken.None);

        Assert.Equal("Bluey", plan.Title);
        Assert.Equal("Mum School", plan.EpisodeTitle);
        Assert.Equal(Path.Combine("Bluey (2018)", "Season 02", "Bluey S02E14 Mum School.mkv"), plan.DestinationRelativePath);
        Assert.Collection(
            plan.NfoSidecars,
            sidecar => Assert.Equal(Path.Combine("Bluey (2018)", "tvshow.nfo"), sidecar.RelativePath),
            sidecar => Assert.Equal(Path.Combine("Bluey (2018)", "Season 02", "Bluey S02E14 Mum School.nfo"), sidecar.RelativePath));
        Assert.True(plan.CanImport);
        Assert.True(File.Exists(Path.Combine(_inboxPath, request.SourceFileName)));
    }

    [Fact]
    public async Task Imports_episode_with_series_and_episode_nfos()
    {
        const string SourceFileName = "Bluey.S02E14.mkv";
        var sourcePath = Path.Combine(_inboxPath, SourceFileName);
        File.WriteAllBytes(sourcePath, new byte[] { 2, 14 });
        using var service = CreateService();
        var request = new ImportRequest
        {
            SourceFileName = SourceFileName,
            MediaType = ImportMediaType.Series,
            TmdbId = "82728",
            SeasonNumber = 2,
            EpisodeNumber = 14
        };

        var result = await service.ImportAsync(request, CancellationToken.None);

        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(result.Plan.DestinationPath));
        var seriesNfoPath = Path.Combine(_seriesPath, "Bluey (2018)", "tvshow.nfo");
        var episodeNfoPath = Path.ChangeExtension(result.Plan.DestinationPath, ".nfo");
        Assert.Contains("<tmdbid>82728</tmdbid>", File.ReadAllText(seriesNfoPath), StringComparison.Ordinal);
        var episodeNfo = File.ReadAllText(episodeNfoPath);
        Assert.Contains("<season>2</season>", episodeNfo, StringComparison.Ordinal);
        Assert.Contains("<episode>14</episode>", episodeNfo, StringComparison.Ordinal);
        Assert.DoesNotContain("<tmdbid>", episodeNfo, StringComparison.Ordinal);
        Assert.Equal(1, _scanService.QueueCount);
    }

    [Fact]
    public async Task Conflicting_existing_nfo_blocks_import_and_preserves_source()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 1 });
        var movieDirectory = Directory.CreateDirectory(Path.Combine(_moviesPath, "Dune Part Two (2024)")).FullName;
        var nfoPath = Path.Combine(movieDirectory, "movie.nfo");
        File.WriteAllText(nfoPath, "<?xml version=\"1.0\"?><movie><tmdbid>438631</tmdbid></movie>");
        using var service = CreateService();

        await Assert.ThrowsAsync<ImportConflictException>(
            () => service.ImportAsync(CreateMovieRequest(), CancellationToken.None));

        Assert.True(File.Exists(sourcePath));
        Assert.Contains("438631", File.ReadAllText(nfoPath), StringComparison.Ordinal);
        Assert.Equal(0, _scanService.QueueCount);
    }

    [Fact]
    public async Task Matching_existing_nfo_is_reused_without_modification()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 1 });
        var movieDirectory = Directory.CreateDirectory(Path.Combine(_moviesPath, "Dune Part Two (2024)")).FullName;
        var nfoPath = Path.Combine(movieDirectory, "movie.nfo");
        const string ExistingNfo = "<?xml version=\"1.0\"?><movie><title>Custom title</title><tmdbid>693134</tmdbid></movie>";
        File.WriteAllText(nfoPath, ExistingNfo);
        using var service = CreateService();

        var result = await service.ImportAsync(CreateMovieRequest(), CancellationToken.None);

        Assert.True(File.Exists(result.Plan.DestinationPath));
        Assert.Equal(ExistingNfo, File.ReadAllText(nfoPath));
        Assert.True(Assert.Single(result.Plan.NfoSidecars).AlreadyExists);
    }

    [Fact]
    public async Task Invalid_tmdb_id_is_rejected_before_any_file_change()
    {
        var sourcePath = Path.Combine(_inboxPath, "title_t00.mkv");
        File.WriteAllBytes(sourcePath, new byte[] { 1 });
        using var service = CreateService();
        var request = CreateMovieRequest();
        request.TmdbId = "../693134";

        await Assert.ThrowsAsync<ImportValidationException>(
            () => service.ImportAsync(request, CancellationToken.None));

        Assert.True(File.Exists(sourcePath));
        Assert.Equal(0, _scanService.QueueCount);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private static ImportRequest CreateMovieRequest()
        => new()
        {
            SourceFileName = "title_t00.mkv",
            MediaType = ImportMediaType.Movie,
            TmdbId = "693134"
        };

    private ImportService CreateService(NamingService? namingService = null)
        => new(
            new TestConfigurationAccessor(_inboxPath, _moviesPath, _seriesPath),
            new TestMetadataSearchService(),
            namingService ?? new NamingService(),
            new NfoService(),
            new ImportPathValidator(),
            new PathGuard(),
            _scanService,
            NullLogger<ImportService>.Instance);

    private sealed class TestConfigurationAccessor : IPluginConfigurationAccessor
    {
        private readonly PluginConfiguration _configuration;

        public TestConfigurationAccessor(string inboxPath, string moviesPath, string seriesPath)
        {
            _configuration = new PluginConfiguration
            {
                InboxPath = inboxPath,
                MoviesLibraryPath = moviesPath,
                SeriesLibraryPath = seriesPath
            };
        }

        public PluginConfiguration GetCurrent() => _configuration;
    }

    private sealed class TestMetadataSearchService : IMetadataSearchService
    {
        public Task<IReadOnlyList<MetadataSearchResult>> SearchMoviesAsync(string title, int? year, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MetadataSearchResult>>([]);

        public Task<IReadOnlyList<MetadataSearchResult>> SearchSeriesAsync(string title, int? year, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MetadataSearchResult>>([]);

        public Task<MetadataSearchResult?> ResolveMovieAsync(string tmdbId, CancellationToken cancellationToken)
            => Task.FromResult<MetadataSearchResult?>(new MetadataSearchResult("Dune: Part Two", 2024, "693134"));

        public Task<MetadataSearchResult?> ResolveSeriesAsync(string tmdbId, CancellationToken cancellationToken)
            => Task.FromResult<MetadataSearchResult?>(new MetadataSearchResult("Bluey", 2018, "82728"));

        public Task<EpisodeResolution?> ResolveEpisodeAsync(
            string seriesTmdbId,
            int seasonNumber,
            int episodeNumber,
            CancellationToken cancellationToken)
            => Task.FromResult<EpisodeResolution?>(new EpisodeResolution("Mum School", seasonNumber, episodeNumber, null));
    }

    private sealed class TestLibraryScanService : ILibraryScanService
    {
        public int QueueCount { get; private set; }

        public bool ThrowOnQueue { get; set; }

        public void QueueScan()
        {
            if (ThrowOnQueue)
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            QueueCount++;
        }
    }
}
