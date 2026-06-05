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
}
