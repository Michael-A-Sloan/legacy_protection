namespace ErsatzTV.Core.Streaming;

public static class IptvViewerClientId
{
    public static string Build(string ipAddress, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ipAddress?.Trim() ?? string.Empty;
        }

        return $"{ipAddress?.Trim()}|{accessToken.Trim()}";
    }

    public static string ParseIpAddress(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return string.Empty;
        }

        int separator = clientId.IndexOf('|');
        return separator < 0 ? clientId.Trim() : clientId[..separator].Trim();
    }
}
