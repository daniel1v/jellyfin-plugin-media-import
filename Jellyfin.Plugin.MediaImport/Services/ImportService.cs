using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Naming;
using Jellyfin.Plugin.MediaImport.Nfo;
using Jellyfin.Plugin.MediaImport.Security;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Implements the preview-first, non-overwriting import workflow.
/// </summary>
public sealed class ImportService : IImportService, IDisposable
{
    private readonly IPluginConfigurationAccessor _configurationAccessor;
    private readonly IMetadataSearchService _metadataSearchService;
    private readonly INamingService _namingService;
    private readonly INfoService _nfoService;
    private readonly IImportPathValidator _importPathValidator;
    private readonly IPathGuard _pathGuard;
    private readonly ILibraryScanService _libraryScanService;
    private readonly ILogger<ImportService> _logger;
    private readonly SemaphoreSlim _importLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportService"/> class.
    /// </summary>
    /// <param name="configurationAccessor">The current plugin configuration.</param>
    /// <param name="metadataSearchService">The Jellyfin metadata search wrapper.</param>
    /// <param name="namingService">The Jellyfin naming service.</param>
    /// <param name="nfoService">The local NFO metadata service.</param>
    /// <param name="importPathValidator">The configured import-root validator.</param>
    /// <param name="pathGuard">The filesystem path guard.</param>
    /// <param name="libraryScanService">The Jellyfin library scan wrapper.</param>
    /// <param name="logger">The logger.</param>
    public ImportService(
        IPluginConfigurationAccessor configurationAccessor,
        IMetadataSearchService metadataSearchService,
        INamingService namingService,
        INfoService nfoService,
        IImportPathValidator importPathValidator,
        IPathGuard pathGuard,
        ILibraryScanService libraryScanService,
        ILogger<ImportService> logger)
    {
        _configurationAccessor = configurationAccessor;
        _metadataSearchService = metadataSearchService;
        _namingService = namingService;
        _nfoService = nfoService;
        _importPathValidator = importPathValidator;
        _pathGuard = pathGuard;
        _libraryScanService = libraryScanService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImportPlan> PreviewAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.MediaType))
        {
            throw new ImportValidationException("The selected media type is invalid.");
        }

        var configuration = _configurationAccessor.GetCurrent();
        ValidateImportPaths(configuration);
        var sourcePath = _pathGuard.ResolveSourcePath(configuration.InboxPath, request.SourceFileName);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var tmdbId = ValidateTmdbId(request.TmdbId);

        return request.MediaType switch
        {
            ImportMediaType.Movie => await CreateMoviePlanAsync(request, tmdbId, configuration.MoviesLibraryPath, extension, cancellationToken).ConfigureAwait(false),
            ImportMediaType.Series => await CreateEpisodePlanAsync(request, tmdbId, configuration.SeriesLibraryPath, extension, cancellationToken).ConfigureAwait(false),
            _ => throw new ImportValidationException("The selected media type is invalid.")
        };
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        await _importLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var plan = await PreviewAsync(request, cancellationToken).ConfigureAwait(false);
            if (!plan.CanImport)
            {
                throw new ImportConflictException(plan.ConflictMessage ?? "The import target already exists.");
            }

            var configuration = _configurationAccessor.GetCurrent();
            ValidateImportPaths(configuration);
            var sourcePath = _pathGuard.ResolveSourcePath(configuration.InboxPath, plan.SourceFileName);
            var createdNfoPaths = new List<string>();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(plan.DestinationPath)!);
                foreach (var document in CreateNfoDocuments(plan))
                {
                    var nfoPath = _pathGuard.ResolveTargetPath(plan.DestinationRoot, document.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(nfoPath)!);
                    _nfoService.ValidateExisting(nfoPath, document);
                    if (File.Exists(nfoPath))
                    {
                        continue;
                    }

                    try
                    {
                        _nfoService.WriteNew(nfoPath, document);
                        createdNfoPaths.Add(nfoPath);
                    }
                    catch (IOException exception) when (File.Exists(nfoPath) || Directory.Exists(nfoPath))
                    {
                        throw new ImportConflictException($"The NFO target '{document.RelativePath}' was created by another operation. Nothing was overwritten.", exception);
                    }
                }

                File.Move(sourcePath, plan.DestinationPath, false);
            }
            catch (IOException) when (File.Exists(plan.DestinationPath) || Directory.Exists(plan.DestinationPath))
            {
                CleanupCreatedNfoFiles(createdNfoPaths);
                throw new ImportConflictException("The import target already exists. No file was overwritten.");
            }
            catch (IOException exception)
            {
                CleanupCreatedNfoFiles(createdNfoPaths);
                throw new ImportOperationException("The media file could not be moved. No existing file was overwritten.", exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                CleanupCreatedNfoFiles(createdNfoPaths);
                throw new ImportOperationException("Jellyfin does not have permission to create the import target.", exception);
            }
            catch (ImportConflictException)
            {
                CleanupCreatedNfoFiles(createdNfoPaths);
                throw;
            }

            var scanQueued = true;
            try
            {
                _libraryScanService.QueueScan();
            }
            catch (Exception exception)
            {
                scanQueued = false;
                _logger.LogError(exception, "The media file was imported, but the Jellyfin library scan could not be queued.");
            }

            return new ImportResult(plan, scanQueued);
        }
        finally
        {
            _importLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _importLock.Dispose();
    }

    private async Task<ImportPlan> CreateMoviePlanAsync(
        ImportRequest request,
        string tmdbId,
        string libraryRoot,
        string extension,
        CancellationToken cancellationToken)
    {
        var movie = await _metadataSearchService.ResolveMovieAsync(tmdbId, cancellationToken).ConfigureAwait(false)
            ?? throw new ImportMetadataException("The selected movie could not be resolved through Jellyfin's TMDb provider.");
        var relativePath = _namingService.GetMovieRelativePath(movie.Name, movie.ProductionYear, extension);
        return CreatePlan(
            request,
            movie.TmdbId,
            movie.Name,
            movie.ProductionYear,
            null,
            null,
            null,
            null,
            libraryRoot,
            relativePath);
    }

    private async Task<ImportPlan> CreateEpisodePlanAsync(
        ImportRequest request,
        string tmdbId,
        string libraryRoot,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!request.SeasonNumber.HasValue || !request.EpisodeNumber.HasValue)
        {
            throw new ImportValidationException("A series import requires a season and episode number.");
        }

        var seriesTask = _metadataSearchService.ResolveSeriesAsync(tmdbId, cancellationToken);
        var episodeTask = _metadataSearchService.ResolveEpisodeAsync(
            tmdbId,
            request.SeasonNumber.Value,
            request.EpisodeNumber.Value,
            cancellationToken);
        await Task.WhenAll(seriesTask, episodeTask).ConfigureAwait(false);

        var series = await seriesTask.ConfigureAwait(false)
            ?? throw new ImportMetadataException("The selected series could not be resolved through Jellyfin's TMDb provider.");
        var episode = await episodeTask.ConfigureAwait(false)
            ?? throw new ImportMetadataException("The selected episode could not be resolved through Jellyfin's TMDb provider.");
        var episodeTmdbId = string.IsNullOrWhiteSpace(episode.TmdbId)
            ? null
            : ValidateTmdbId(episode.TmdbId);
        var relativePath = _namingService.GetEpisodeRelativePath(
            series.Name,
            series.ProductionYear,
            episode.SeasonNumber,
            episode.EpisodeNumber,
            episode.Name,
            extension);
        return CreatePlan(
            request,
            series.TmdbId,
            series.Name,
            series.ProductionYear,
            episode.SeasonNumber,
            episode.EpisodeNumber,
            episode.Name,
            episodeTmdbId,
            libraryRoot,
            relativePath);
    }

    private ImportPlan CreatePlan(
        ImportRequest request,
        string tmdbId,
        string title,
        int? year,
        int? seasonNumber,
        int? episodeNumber,
        string? episodeTitle,
        string? episodeTmdbId,
        string libraryRoot,
        string relativePath)
    {
        var destinationPath = _pathGuard.ResolveTargetPath(libraryRoot, relativePath);
        var conflictMessage = File.Exists(destinationPath) || Directory.Exists(destinationPath)
            ? "The import target already exists. No file will be overwritten."
            : null;
        var documents = CreateNfoDocuments(
            request.MediaType,
            relativePath,
            title,
            year,
            tmdbId,
            seasonNumber,
            episodeNumber,
            episodeTitle,
            episodeTmdbId);
        var sidecars = new List<NfoSidecarPlan>(documents.Count);
        foreach (var document in documents)
        {
            var nfoPath = _pathGuard.ResolveTargetPath(libraryRoot, document.RelativePath);
            var alreadyExists = File.Exists(nfoPath);
            try
            {
                _nfoService.ValidateExisting(nfoPath, document);
            }
            catch (ImportConflictException exception)
            {
                conflictMessage ??= exception.Message;
            }

            sidecars.Add(new NfoSidecarPlan(document.RelativePath, nfoPath, alreadyExists));
        }

        return new ImportPlan(
            request.SourceFileName,
            request.MediaType,
            tmdbId,
            title,
            year,
            seasonNumber,
            episodeNumber,
            episodeTitle,
            episodeTmdbId,
            Path.GetFullPath(libraryRoot),
            relativePath,
            destinationPath,
            sidecars,
            conflictMessage is null,
            conflictMessage);
    }

    private IReadOnlyList<NfoDocument> CreateNfoDocuments(ImportPlan plan)
        => CreateNfoDocuments(
            plan.MediaType,
            plan.DestinationRelativePath,
            plan.Title,
            plan.Year,
            plan.TmdbId,
            plan.SeasonNumber,
            plan.EpisodeNumber,
            plan.EpisodeTitle,
            plan.EpisodeTmdbId);

    private IReadOnlyList<NfoDocument> CreateNfoDocuments(
        ImportMediaType mediaType,
        string relativePath,
        string title,
        int? year,
        string tmdbId,
        int? seasonNumber,
        int? episodeNumber,
        string? episodeTitle,
        string? episodeTmdbId)
        => mediaType switch
        {
            ImportMediaType.Movie => _nfoService.CreateMovieDocuments(relativePath, title, year, tmdbId),
            ImportMediaType.Series => _nfoService.CreateEpisodeDocuments(
                relativePath,
                title,
                year,
                tmdbId,
                seasonNumber ?? throw new ImportValidationException("The resolved season number is missing."),
                episodeNumber ?? throw new ImportValidationException("The resolved episode number is missing."),
                episodeTitle ?? throw new ImportValidationException("The resolved episode title is missing."),
                episodeTmdbId),
            _ => throw new ImportValidationException("The selected media type is invalid.")
        };

    private void CleanupCreatedNfoFiles(IReadOnlyList<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(exception, "Could not remove newly created NFO sidecar {NfoPath} after a failed import.", path);
            }
        }
    }

    private static string ValidateTmdbId(string tmdbId)
    {
        if (!int.TryParse(tmdbId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedId) || parsedId <= 0)
        {
            throw new ImportValidationException("The selected TMDb provider ID is invalid.");
        }

        return parsedId.ToString(CultureInfo.InvariantCulture);
    }

    private void ValidateImportPaths(Configuration.PluginConfiguration configuration)
        => _importPathValidator.Validate(
            configuration.InboxPath,
            configuration.MoviesLibraryPath,
            configuration.SeriesLibraryPath);
}
