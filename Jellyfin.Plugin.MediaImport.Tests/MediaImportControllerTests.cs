using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Configuration;
using Jellyfin.Plugin.MediaImport.Controllers;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public class MediaImportControllerTests
{
    [Fact]
    public async Task Empty_movie_search_returns_neutral_provider_message()
    {
        var controller = CreateController(new EmptyInboxService());

        var action = await controller.SearchMovies("Unknown", null, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var body = Assert.IsType<MetadataSearchResponse<MetadataSearchResult>>(response.Value);
        Assert.Empty(body.Results);
        Assert.Contains("No results were obtained", body.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("offline", body.Message, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Unconfigured_inbox_returns_bad_request_without_path_details()
    {
        var controller = CreateController(new FailingInboxService());

        var action = controller.GetFiles();

        var response = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, response.StatusCode);
        var body = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal("The Media Import inbox is not configured.", body.Detail);
    }

    [Fact]
    public void Proposed_overlapping_paths_are_rejected_before_saving()
    {
        var controller = CreateController(new EmptyInboxService());
        var root = Path.Combine(Path.GetTempPath(), "media-import-controller-paths");

        var action = controller.ValidateConfiguration(
            new ImportPathConfiguration(
                root,
                Path.Combine(root, "movies"),
                Path.Combine(Path.GetTempPath(), "series")));

        var response = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<ImportPathValidationResult>(response.Value);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Existing_overlapping_configuration_blocks_queue_access()
    {
        var controller = new MediaImportController(
            new EmptyInboxService(),
            new EmptyInboxMediaInfoService(),
            new EmptyMetadataSearchService(),
            new OverlappingConfigurationAccessor(),
            new EmptyImportService(),
            new ImportPathValidator());

        var action = controller.GetFiles();

        var response = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, response.StatusCode);
        var body = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Contains("must not contain", body.Detail, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_video_returns_controlled_media_info_response()
    {
        var root = Path.Combine(Path.GetTempPath(), $"media-import-controller-{System.Guid.NewGuid():N}");
        var inbox = Directory.CreateDirectory(Path.Combine(root, "inbox")).FullName;
        var movies = Directory.CreateDirectory(Path.Combine(root, "movies")).FullName;
        var series = Directory.CreateDirectory(Path.Combine(root, "series")).FullName;
        try
        {
            var controller = new MediaImportController(
                new EmptyInboxService(),
                new FailingInboxMediaInfoService(),
                new EmptyMetadataSearchService(),
                new StaticConfigurationAccessor(inbox, movies, series),
                new EmptyImportService(),
                new ImportPathValidator());

            var action = await controller.GetMediaInfo("broken.mkv", CancellationToken.None);

            var response = Assert.IsType<ObjectResult>(action.Result);
            Assert.Equal(422, response.StatusCode);
            var body = Assert.IsType<ProblemDetails>(response.Value);
            Assert.Equal("The selected inbox file could not be analyzed as a video.", body.Detail);
            Assert.DoesNotContain(root, body.Detail, System.StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static MediaImportController CreateController(IInboxService inboxService)
        => new(
            inboxService,
            new EmptyInboxMediaInfoService(),
            new EmptyMetadataSearchService(),
            new TestConfigurationAccessor(),
            new EmptyImportService(),
            new ImportPathValidator());

    private sealed class EmptyInboxService : IInboxService
    {
        public IReadOnlyList<InboxFile> GetFiles(string inboxPath) => [];
    }

    private sealed class FailingInboxService : IInboxService
    {
        public IReadOnlyList<InboxFile> GetFiles(string inboxPath)
            => throw new System.InvalidOperationException("The Media Import inbox is not configured.");
    }

    private sealed class EmptyInboxMediaInfoService : IInboxMediaInfoService
    {
        public Task<InboxMediaInfo> GetAsync(
            string inboxPath,
            string sourceFileName,
            CancellationToken cancellationToken)
            => Task.FromResult(new InboxMediaInfo(sourceFileName, null, null, null));
    }

    private sealed class FailingInboxMediaInfoService : IInboxMediaInfoService
    {
        public Task<InboxMediaInfo> GetAsync(
            string inboxPath,
            string sourceFileName,
            CancellationToken cancellationToken)
            => throw new MediaProbeException(
                "The selected inbox file could not be analyzed as a video.",
                new System.InvalidOperationException("Expected test failure."));
    }

    private sealed class EmptyMetadataSearchService : IMetadataSearchService
    {
        public Task<IReadOnlyList<MetadataSearchResult>> SearchMoviesAsync(
            string title,
            int? year,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MetadataSearchResult>>([]);

        public Task<IReadOnlyList<MetadataSearchResult>> SearchSeriesAsync(
            string title,
            int? year,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<MetadataSearchResult>>([]);

        public Task<MetadataSearchResult?> ResolveMovieAsync(string tmdbId, CancellationToken cancellationToken)
            => Task.FromResult<MetadataSearchResult?>(null);

        public Task<MetadataSearchResult?> ResolveSeriesAsync(string tmdbId, CancellationToken cancellationToken)
            => Task.FromResult<MetadataSearchResult?>(null);

        public Task<EpisodeResolution?> ResolveEpisodeAsync(
            string seriesTmdbId,
            int seasonNumber,
            int episodeNumber,
            CancellationToken cancellationToken)
            => Task.FromResult<EpisodeResolution?>(null);
    }

    private sealed class TestConfigurationAccessor : IPluginConfigurationAccessor
    {
        public PluginConfiguration GetCurrent() => new();
    }

    private sealed class OverlappingConfigurationAccessor : IPluginConfigurationAccessor
    {
        public PluginConfiguration GetCurrent()
        {
            var root = Path.Combine(Path.GetTempPath(), "media-import-controller-overlap");
            return new PluginConfiguration
            {
                InboxPath = root,
                MoviesLibraryPath = Path.Combine(root, "movies"),
                SeriesLibraryPath = Path.Combine(Path.GetTempPath(), "series")
            };
        }
    }

    private sealed class StaticConfigurationAccessor : IPluginConfigurationAccessor
    {
        private readonly PluginConfiguration _configuration;

        public StaticConfigurationAccessor(string inboxPath, string moviesPath, string seriesPath)
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

    private sealed class EmptyImportService : IImportService
    {
        public Task<ImportPlan> PreviewAsync(ImportRequest request, CancellationToken cancellationToken)
            => throw new System.NotSupportedException();

        public Task<ImportResult> ImportAsync(ImportRequest request, CancellationToken cancellationToken)
            => throw new System.NotSupportedException();
    }
}
