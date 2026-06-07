using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Application.Security;

public record ScanLoginIpsForAutoBan : IRequest<LoginIpAutoBanScanResult>;

public class ScanLoginIpsForAutoBanHandler(ILoginIpAutoBanScanner scanner)
    : IRequestHandler<ScanLoginIpsForAutoBan, LoginIpAutoBanScanResult>
{
    public Task<LoginIpAutoBanScanResult> Handle(
        ScanLoginIpsForAutoBan request,
        CancellationToken cancellationToken) =>
        scanner.ScanAsync(cancellationToken);
}
