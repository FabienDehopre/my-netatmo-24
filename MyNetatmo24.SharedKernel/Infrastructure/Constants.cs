namespace MyNetatmo24.SharedKernel.Infrastructure;

public static class Constants
{
    public const string SoftDeleteFilter = "SoftDeleteFilter";
    public const string DatabaseName = "my-netatmo24-db";
    public const string CacheName = "cache";

    /// <summary>
    /// The hosting environment the integration-test harness runs the application under.
    /// </summary>
    public const string IntegrationTestEnvironmentName = "IntegrationTest";

    public static class Policies
    {
        public const string ReadWeather = "ReadWeather";
    }
}
