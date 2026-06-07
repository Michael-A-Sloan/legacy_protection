namespace ErsatzTV.Core.Interfaces.Security;

public record LoginIpAutoBanAction(string IpAddress, string Reason);

public record LoginIpAutoBanScanResult(
    int ScannedIpCount,
    int BannedCount,
    int SkippedCount,
    IReadOnlyList<LoginIpAutoBanAction> Actions);

public interface ILoginIpAutoBanScanner
{
    Task<LoginIpAutoBanScanResult> ScanAsync(CancellationToken cancellationToken);
}
