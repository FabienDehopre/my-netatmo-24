namespace MyNetatmo24.SharedKernel.Infrastructure;

public static class Constants
{
    public const string SoftDeleteFilter = "SoftDeleteFilter";
    public const string DatabaseName = "my-netatmo24-db";
    public const string CacheName = "cache";

    public static class Policies
    {
        public const string ReadWeather = "ReadWeather";
    }
}
