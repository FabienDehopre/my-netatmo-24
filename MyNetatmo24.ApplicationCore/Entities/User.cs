namespace MyNetatmo24.ApplicationCore.Entities;

public class User
{
    public long Id { get; set; }
    public string Auth0UserId { get; set; } = null!;
    public string NetatmoAccessToken { get; set; } = null!;
    public string NetatmoRefreshToken { get; set; } = null!;
    public DateTime Inserted { get; set; }
    public DateTime LastUpdated { get; set; }
}