using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Application.Security;

public class AdminIpRuleViewModel
{
    public int Id { get; set; }
    public string IpAddress { get; set; }
    public string IpAddressV4 { get; set; }
    public string IpAddressV6 { get; set; }
    public AdminIpRuleType RuleType { get; set; }
    public string Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PageViewCount { get; set; }
    public int LoginAttemptCount { get; set; }
    public int AccessDeniedCount { get; set; }
    public int TotalActivityCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsTor { get; set; }
    public bool IsVpnOrProxy => IsVpn || IsProxy || IsTor;
    public bool BlockIptvStreaming { get; set; }
}
