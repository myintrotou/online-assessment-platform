using Npgsql;

namespace AssessmentPlatform.Api.Common;

/// <summary>
/// Neon and Render hand out their database credentials as a single "postgres://user:pass@host/db" URL.
/// Npgsql wants a key=value connection string, so this converts one to the other.
/// </summary>
public static class NpgsqlUrl
{
    public static string ToConnectionString(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
