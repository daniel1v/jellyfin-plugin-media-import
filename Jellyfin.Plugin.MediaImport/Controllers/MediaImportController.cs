using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaImport.Controllers;

/// <summary>
/// Provides administrator-only operations for the interactive import workflow.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("MediaImport")]
public sealed class MediaImportController : ControllerBase
{
    private const string NoTmdbResultsMessage =
        "No results were obtained from the TMDb provider. If this persists, review Jellyfin's metadata provider configuration and server logs.";

    private readonly IInboxService _inboxService;
    private readonly IInboxMediaInfoService _inboxMediaInfoService;
    private readonly IMetadataSearchService _metadataSearchService;
    private readonly IPluginConfigurationAccessor _configurationAccessor;
    private readonly IImportService _importService;
    private readonly IImportPathValidator _importPathValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaImportController"/> class.
    /// </summary>
    /// <param name="inboxService">The inbox reader.</param>
    /// <param name="inboxMediaInfoService">The cached media probe coordinator.</param>
    /// <param name="metadataSearchService">The Jellyfin metadata search wrapper.</param>
    /// <param name="configurationAccessor">The current plugin configuration.</param>
    /// <param name="importService">The preview and import service.</param>
    /// <param name="importPathValidator">The configured import-root validator.</param>
    public MediaImportController(
        IInboxService inboxService,
        IInboxMediaInfoService inboxMediaInfoService,
        IMetadataSearchService metadataSearchService,
        IPluginConfigurationAccessor configurationAccessor,
        IImportService importService,
        IImportPathValidator importPathValidator)
    {
        _inboxService = inboxService;
        _inboxMediaInfoService = inboxMediaInfoService;
        _metadataSearchService = metadataSearchService;
        _configurationAccessor = configurationAccessor;
        _importService = importService;
        _importPathValidator = importPathValidator;
    }

    /// <summary>
    /// Probes one inbox file for duration and video dimensions.
    /// </summary>
    /// <param name="sourceFileName">The filename relative to the inbox.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The available technical video details.</returns>
    [HttpGet("Files/MediaInfo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InboxMediaInfo>> GetMediaInfo(
        [FromQuery, Required] string sourceFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = _configurationAccessor.GetCurrent();
            _importPathValidator.Validate(
                configuration.InboxPath,
                configuration.MoviesLibraryPath,
                configuration.SeriesLibraryPath);
            return Ok(await _inboxMediaInfoService
                .GetAsync(configuration.InboxPath, sourceFileName, cancellationToken)
                .ConfigureAwait(false));
        }
        catch (ImportValidationException exception)
        {
            return ImportProblem(StatusCodes.Status400BadRequest, "Invalid inbox file", exception.Message);
        }
        catch (ImportConflictException exception)
        {
            return ImportProblem(StatusCodes.Status409Conflict, "Import queue changed", exception.Message);
        }
        catch (MediaProbeException exception)
        {
            return ImportProblem(
                StatusCodes.Status422UnprocessableEntity,
                "Media information unavailable",
                exception.Message);
        }
    }

    /// <summary>
    /// Lists supported video files waiting in the configured inbox.
    /// </summary>
    /// <response code="200">The inbox was read successfully.</response>
    /// <response code="400">The inbox is not configured or unavailable.</response>
    /// <returns>The import queue.</returns>
    [HttpGet("Files")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<InboxFile>> GetFiles()
    {
        try
        {
            var configuration = _configurationAccessor.GetCurrent();
            _importPathValidator.Validate(
                configuration.InboxPath,
                configuration.MoviesLibraryPath,
                configuration.SeriesLibraryPath);
            return Ok(_inboxService.GetFiles(configuration.InboxPath));
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "Media Import inbox unavailable",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ImportValidationException exception)
        {
            return Problem(
                title: "Media Import paths overlap",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (IOException)
        {
            return Problem(
                title: "Media Import inbox unavailable",
                detail: "The configured Media Import inbox could not be read.",
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (UnauthorizedAccessException)
        {
            return Problem(
                title: "Media Import inbox unavailable",
                detail: "Jellyfin does not have permission to read the configured Media Import inbox.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Validates proposed import roots before saving the plugin configuration.
    /// </summary>
    /// <param name="configuration">The proposed import roots.</param>
    /// <returns>The validation result.</returns>
    [HttpPost("Configuration/Validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ImportPathValidationResult> ValidateConfiguration(
        [FromBody, Required] ImportPathConfiguration configuration)
    {
        try
        {
            _importPathValidator.Validate(
                configuration.InboxPath,
                configuration.MoviesLibraryPath,
                configuration.SeriesLibraryPath);
            return Ok(new ImportPathValidationResult(true, null));
        }
        catch (ImportValidationException exception)
        {
            return Ok(new ImportPathValidationResult(false, exception.Message));
        }
    }

    /// <summary>
    /// Searches films using Jellyfin's enabled TMDb provider.
    /// </summary>
    /// <param name="query">The film title.</param>
    /// <param name="year">The optional release year.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The TMDb search results and a neutral message when none were obtained.</returns>
    [HttpGet("Search/Movies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MetadataSearchResponse<MetadataSearchResult>>> SearchMovies(
        [FromQuery, Required] string query,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var results = await _metadataSearchService
            .SearchMoviesAsync(query, year, cancellationToken)
            .ConfigureAwait(false);
        return Ok(CreateSearchResponse(results));
    }

    /// <summary>
    /// Searches series using Jellyfin's enabled TMDb provider.
    /// </summary>
    /// <param name="query">The series title.</param>
    /// <param name="year">The optional first-air year.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The TMDb search results and a neutral message when none were obtained.</returns>
    [HttpGet("Search/Series")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MetadataSearchResponse<MetadataSearchResult>>> SearchSeries(
        [FromQuery, Required] string query,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var results = await _metadataSearchService
            .SearchSeriesAsync(query, year, cancellationToken)
            .ConfigureAwait(false);
        return Ok(CreateSearchResponse(results));
    }

    /// <summary>
    /// Resolves a selected series' episode using explicit season and episode numbers.
    /// </summary>
    /// <param name="seriesTmdbId">The selected series' TMDb provider ID.</param>
    /// <param name="seasonNumber">The season number, including zero for specials.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The resolved episode and a neutral message when no result was obtained.</returns>
    [HttpGet("Search/Episode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MetadataSearchResponse<EpisodeResolution>>> ResolveEpisode(
        [FromQuery, Required, RegularExpression("^[1-9][0-9]*$")] string seriesTmdbId,
        [FromQuery, Range(0, int.MaxValue)] int seasonNumber,
        [FromQuery, Range(1, int.MaxValue)] int episodeNumber,
        CancellationToken cancellationToken)
    {
        var result = await _metadataSearchService
            .ResolveEpisodeAsync(seriesTmdbId, seasonNumber, episodeNumber, cancellationToken)
            .ConfigureAwait(false);
        var results = result is null ? Array.Empty<EpisodeResolution>() : new[] { result };
        return Ok(CreateSearchResponse(results));
    }

    /// <summary>
    /// Builds a server-controlled import preview without changing files.
    /// </summary>
    /// <param name="request">The selected inbox file and metadata.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The reviewable import plan.</returns>
    [HttpPost("Preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImportPlan>> Preview(
        [FromBody, Required] ImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _importService.PreviewAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (ImportValidationException exception)
        {
            return ImportProblem(StatusCodes.Status400BadRequest, "Invalid import preview", exception.Message);
        }
        catch (ImportMetadataException exception)
        {
            return ImportProblem(StatusCodes.Status422UnprocessableEntity, "Metadata could not be resolved", exception.Message);
        }
        catch (ImportConflictException exception)
        {
            return ImportProblem(StatusCodes.Status409Conflict, "Import queue changed", exception.Message);
        }
    }

    /// <summary>
    /// Revalidates and executes one explicitly confirmed import without overwriting.
    /// </summary>
    /// <param name="request">The confirmed inbox file and metadata.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The completed import result.</returns>
    [HttpPost("Import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImportResult>> Import(
        [FromBody, Required] ImportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _importService.ImportAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (ImportValidationException exception)
        {
            return ImportProblem(StatusCodes.Status400BadRequest, "Invalid import request", exception.Message);
        }
        catch (ImportMetadataException exception)
        {
            return ImportProblem(StatusCodes.Status422UnprocessableEntity, "Metadata could not be resolved", exception.Message);
        }
        catch (ImportConflictException exception)
        {
            return ImportProblem(StatusCodes.Status409Conflict, "Import conflict", exception.Message);
        }
        catch (ImportOperationException exception)
        {
            return ImportProblem(StatusCodes.Status500InternalServerError, "Import failed", exception.Message);
        }
    }

    private static MetadataSearchResponse<T> CreateSearchResponse<T>(IReadOnlyList<T> results)
        => new(results, results.Count == 0 ? NoTmdbResultsMessage : null);

    private ObjectResult ImportProblem(int statusCode, string title, string detail)
        => Problem(title: title, detail: detail, statusCode: statusCode);
}
