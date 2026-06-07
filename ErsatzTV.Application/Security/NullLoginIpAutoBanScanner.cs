using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Application.Security;

public sealed class NullLoginIpAutoBanScanner : ILoginIpAutoBanScanner
{
    public Task<LoginIpAutoBanScanResult> ScanAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new LoginIpAutoBanScanResult(0, 0, 0, []));
}
