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

        try
        {
            await publicBlocklistService.RefreshDueListsAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Initial public blocklist refresh failed");
        }

        using PeriodicTimer timer = new(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await publicBlocklistService.RefreshDueListsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Public blocklist refresh cycle failed");
            }
        }
    }
}
