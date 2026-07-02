using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Services;

public sealed class PublicBlocklistUpdaterService(
    IPublicBlocklistService publicBlocklistService,
    ILogger<PublicBlocklistUpdaterService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Public blocklist updater started");

        await RunRefreshCycleAsync(stoppingToken);

        using PeriodicTimer timer = new(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunRefreshCycleAsync(stoppingToken);
        }
    }

    private async Task RunRefreshCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await publicBlocklistService.RefreshDueListsAsync(stoppingToken);
        }
        catch (Exception ex) when (BackgroundServiceExceptionHelper.IsShutdownCancellation(ex, stoppingToken))
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Public blocklist refresh cycle failed");
        }
    }
}
