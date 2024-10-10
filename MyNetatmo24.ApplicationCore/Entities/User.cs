namespace MyNetatmo24.ApplicationCore.Entities;

public class User
{
    public long Id { get; set; }
    public string Auth0UserId { get; set; }
    public string NetatmoAccessToken { get; set; }
    public string NetatmoRefreshToken { get; set; }
    public DateTime Inserted { get; set; }
    public DateTime LastUpdated { get; set; }
}