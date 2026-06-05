namespace ErsatzTV.Core.Domain.Security;

public class AdminLoginAttempt
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    public string IpAddressV4 { get; set; }
    public string IpAddressV6 { get; set; }
    public string Username { get; set; }
    public bool Success { get; set; }
    public string DenyReason { get; set; }
    public string UserAgent { get; set; }
    public AdminLoginAttemptKind AttemptKind { get; set; }
    public string RequestPath { get; set; }
}
