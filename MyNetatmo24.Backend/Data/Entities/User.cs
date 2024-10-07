namespace MyNetatmo24.Backend.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Auth0UserId { get; set; }
    public string NetatmoAccessToken { get; set; }
    public string NetatmoRefreshToken { get; set; }
}