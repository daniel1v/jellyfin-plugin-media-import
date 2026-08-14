using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;
using Jellyfin.Plugin.MediaImport.Security;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Coordinates and caches media probes without delaying the inbox listing.
/// </summary>
public sealed class InboxMediaInfoService : IInboxMediaInfoService, IDisposable
{
    private const int MaximumConcurrentProbes = 2;
    private const int MaximumCachedFiles = 512;
    private readonly ConcurrentDictionary<string, CachedMediaInfo> _cache;
    private readonly SemaphoreSlim _probeGate = new(MaximumConcurrentProbes, MaximumConcurrentProbes);
    private readonly IPathGuard _pathGuard;
    private readonly IMediaProbe _mediaProbe;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxMediaInfoService"/> class.
    /// </summary>
    /// <param name="pathGuard">The source path validator.</param>
    /// <param name="mediaProbe">The Jellyfin media probe adapter.</param>
    public InboxMediaInfoService(IPathGuard pathGuard, IMediaProbe mediaProbe)
    {
        _pathGuard = pathGuard;
        _mediaProbe = mediaProbe;
        _cache = new ConcurrentDictionary<string, CachedMediaInfo>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public async Task<InboxMediaInfo> GetAsync(
        string inboxPath,
        string sourceFileName,
        CancellationToken cancellationToken)
    {
        var sourcePath = _pathGuard.ResolveSourcePath(inboxPath, sourceFileName);
        var fingerprint = CreateFingerprint(sourcePath);
        if (TryGetCached(sourcePath, fingerprint, out var cached))
        {
            return ToInboxMediaInfo(sourceFileName, cached);
        }

        await _probeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            fingerprint = CreateFingerprint(sourcePath);
            if (TryGetCached(sourcePath, fingerprint, out cached))
            {
                return ToInboxMediaInfo(sourceFileName, cached);
            }

            var result = await _mediaProbe.ProbeAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            if (_cache.Count >= MaximumCachedFiles && !_cache.ContainsKey(sourcePath))
            {
                _cache.Clear();
            }

            _cache[sourcePath] = new CachedMediaInfo(fingerprint, result);
            return ToInboxMediaInfo(sourceFileName, result);
        }
        finally
        {
            _probeGate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _probeGate.Dispose();
    }

    private static FileFingerprint CreateFingerprint(string sourcePath)
    {
        var file = new FileInfo(sourcePath);
        return new FileFingerprint(file.Length, file.LastWriteTimeUtc);
    }

    private static InboxMediaInfo ToInboxMediaInfo(string fileName, MediaProbeResult result)
        => new(fileName, result.RunTimeTicks, result.Width, result.Height);

    private bool TryGetCached(string sourcePath, FileFingerprint fingerprint, out MediaProbeResult result)
    {
        if (_cache.TryGetValue(sourcePath, out var cached) && cached.Fingerprint == fingerprint)
        {
            result = cached.Result;
            return true;
        }

        result = null!;
        return false;
    }

    private sealed record CachedMediaInfo(FileFingerprint Fingerprint, MediaProbeResult Result);

    private sealed record FileFingerprint(long SizeBytes, DateTime LastWriteTimeUtc);
}
