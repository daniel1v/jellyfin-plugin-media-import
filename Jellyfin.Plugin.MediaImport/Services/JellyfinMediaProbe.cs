using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Uses Jellyfin's public ffprobe abstraction for local video files.
/// </summary>
public sealed class JellyfinMediaProbe : IMediaProbe
{
    private readonly IMediaEncoder _mediaEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediaProbe"/> class.
    /// </summary>
    /// <param name="mediaEncoder">Jellyfin's media encoder abstraction.</param>
    public JellyfinMediaProbe(IMediaEncoder mediaEncoder)
    {
        _mediaEncoder = mediaEncoder;
    }

    /// <inheritdoc />
    public async Task<MediaProbeResult> ProbeAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var mediaInfo = await _mediaEncoder.GetMediaInfo(
                new MediaInfoRequest
                {
                    MediaType = DlnaProfileType.Video,
                    MediaSource = new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = MediaProtocol.File
                    }
                },
                cancellationToken).ConfigureAwait(false);

            var videoStream = mediaInfo.MediaStreams.FirstOrDefault(stream => stream.Type == MediaStreamType.Video);
            return new MediaProbeResult(mediaInfo.RunTimeTicks, videoStream?.Width, videoStream?.Height);
        }
        catch (FfmpegException exception)
        {
            throw new MediaProbeException("The selected inbox file could not be analyzed as a video.", exception);
        }
    }
}
