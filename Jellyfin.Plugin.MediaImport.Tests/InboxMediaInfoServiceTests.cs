using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class InboxMediaInfoServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"media-import-info-{Guid.NewGuid():N}");

    public InboxMediaInfoServiceTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public async Task Reuses_probe_result_while_file_fingerprint_is_unchanged()
    {
        File.WriteAllText(Path.Combine(_root, "movie.mkv"), "video");
        var probe = new CountingMediaProbe();
        using var service = new InboxMediaInfoService(new PathGuard(), probe);

        var first = await service.GetAsync(_root, "movie.mkv", CancellationToken.None);
        var second = await service.GetAsync(_root, "movie.mkv", CancellationToken.None);

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(first, second);
        Assert.Equal(TimeSpan.FromMinutes(90).Ticks, first.RunTimeTicks);
        Assert.Equal(1920, first.Width);
        Assert.Equal(1080, first.Height);
    }

    [Fact]
    public async Task Probes_again_after_file_size_changes()
    {
        var path = Path.Combine(_root, "movie.mkv");
        File.WriteAllText(path, "video");
        var probe = new CountingMediaProbe();
        using var service = new InboxMediaInfoService(new PathGuard(), probe);

        await service.GetAsync(_root, "movie.mkv", CancellationToken.None);
        File.AppendAllText(path, "-changed");
        await service.GetAsync(_root, "movie.mkv", CancellationToken.None);

        Assert.Equal(2, probe.CallCount);
    }

    [Fact]
    public async Task Limits_parallel_probes_to_two()
    {
        var fileNames = Enumerable.Range(0, 6).Select(index => $"movie-{index}.mkv").ToArray();
        foreach (var fileName in fileNames)
        {
            File.WriteAllText(Path.Combine(_root, fileName), "video");
        }

        var probe = new CountingMediaProbe(TimeSpan.FromMilliseconds(40));
        using var service = new InboxMediaInfoService(new PathGuard(), probe);

        await Task.WhenAll(fileNames.Select(fileName => service.GetAsync(_root, fileName, CancellationToken.None)));

        Assert.Equal(2, probe.MaximumConcurrency);
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private sealed class CountingMediaProbe : IMediaProbe
    {
        private readonly TimeSpan _delay;
        private int _callCount;
        private int _currentConcurrency;
        private int _maximumConcurrency;

        public CountingMediaProbe(TimeSpan delay = default)
        {
            _delay = delay;
        }

        public int CallCount => _callCount;

        public int MaximumConcurrency => _maximumConcurrency;

        public async Task<MediaProbeResult> ProbeAsync(string path, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            var concurrency = Interlocked.Increment(ref _currentConcurrency);
            UpdateMaximumConcurrency(concurrency);
            try
            {
                if (_delay > TimeSpan.Zero)
                {
                    await Task.Delay(_delay, cancellationToken);
                }

                return new MediaProbeResult(TimeSpan.FromMinutes(90).Ticks, 1920, 1080);
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
        }

        private void UpdateMaximumConcurrency(int concurrency)
        {
            int current;
            do
            {
                current = _maximumConcurrency;
                if (current >= concurrency)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref _maximumConcurrency, concurrency, current) != current);
        }
    }
}
