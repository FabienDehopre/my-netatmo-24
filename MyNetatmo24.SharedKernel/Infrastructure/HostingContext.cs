namespace MyNetatmo24.SharedKernel.Infrastructure;

/// <summary>
/// Tells apart the ways the application gets built.
/// </summary>
public static class HostingContext
{
    /// <summary>
    /// Whether the process was launched by the OpenAPI document generator rather than to serve traffic.
    /// The generator builds <em>and starts</em> the application, so wiring that needs real infrastructure
    /// - a database, an identity provider - has to stand down or the build breaks.
    /// </summary>
    public static bool IsGeneratingOpenApiDocument =>
        Environment.GetCommandLineArgs()
            .Any(arg => arg.Contains("GetDocument.Insider", StringComparison.OrdinalIgnoreCase));
}
