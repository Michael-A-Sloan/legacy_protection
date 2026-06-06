using ErsatzTV.Application.Streaming;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class SessionController(IFFmpegSegmenterService ffmpegSegmenterService, IMediator mediator)
{
    [HttpGet("api/sessions")]
    [Tags("Sessions")]
    [EndpointSummary("Get sessions")]
    public List<HlsSessionModel> GetSessions() => ffmpegSegmenterService.Workers.Map(w => w.GetModel()).ToList();

    [HttpGet("api/iptv/viewers")]
    [Tags("Sessions")]
    [EndpointSummary("Get active IPTV viewer counts by channel number")]
    public Task<IReadOnlyDictionary<string, int>> GetActiveViewers(CancellationToken cancellationToken) =>
        mediator.Send(new GetActiveIptvViewerCounts(), cancellationToken);

    [HttpDelete("api/session/{channelNumber}")]
    [Tags("Sessions")]
    [EndpointSummary("Stop session")]
    public async Task<IActionResult> StopSession(string channelNumber, CancellationToken cancellationToken)
    {
        if (await ffmpegSegmenterService.StopChannel(channelNumber, cancellationToken))
        {
            return new NoContentResult();
        }

        return new NotFoundResult();
    }
}
