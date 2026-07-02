using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Services;

public sealed class LoginIpAutoBanScannerService(
    ILoginIpAutoBanScanner scanner,
    IConfigElementRepository configElementRepository,
    ILogger<LoginIpAutoBanScannerService> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Login IP auto-ban scanner started");

        using PeriodicTimer timer = new(ScanInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScanCycleAsync(stoppingToken);
        }
    }

    private async Task RunScanCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            LoginIpAutoBanSettings settings =
                await LoginIpAutoBanSettings.LoadAsync(configElementRepository, stoppingToken);

            if (!settings.IsAnyScanEnabled)
            {
                return;
            }

            await scanner.ScanAsync(stoppingToken);
        }
        catch (Exception ex) when (BackgroundServiceExceptionHelper.IsShutdownCancellation(ex, stoppingToken))
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Login IP auto-ban scan cycle failed");
        }
    }
}
