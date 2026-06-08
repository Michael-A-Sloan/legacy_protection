namespace ErsatzTV.Core.Domain.Security;

public class AdminIpRule
{
    public int Id { get; set; }
    public string IpAddress { get; set; }
    public AdminIpRuleType RuleType { get; set; }
    public string Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool BlockIptvStreaming { get; set; }
}
