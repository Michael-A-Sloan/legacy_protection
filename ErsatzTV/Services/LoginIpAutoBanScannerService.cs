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
            try
            {
                LoginIpAutoBanSettings settings =
                    await LoginIpAutoBanSettings.LoadAsync(configElementRepository, stoppingToken);

                if (!settings.IsAnyScanEnabled)
                {
                    continue;
                }

                await scanner.ScanAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Login IP auto-ban scan cycle failed");
            }
        }
    }
}
