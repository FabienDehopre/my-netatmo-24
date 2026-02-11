using Aspire.Hosting.Lifecycle;

namespace MyNetatmo24.AppHost.Extensions;

internal static class OpenTelemetryCollectorExtensions
{
    private const string DashboardOtlpUrlVariableName = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
    private const string DashboardOtlpKeyVariableName = "AppHost:OtlpApiKey";

    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<OpenTelemetryCollectorResource> AddOpenTelemetryCollector(string otelConfig)
        {
            var collectorResource = new OpenTelemetryCollectorResource("otelcollector");
            var dashboardUrl = builder.Configuration[DashboardOtlpUrlVariableName] ?? "";
            var dashboardOtlpEndpoint = new HostUrl(dashboardUrl);

            var otel = builder
                .AddResource(collectorResource)
                .WithImage("ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib", "latest")
                .WithHttpEndpoint(targetPort: 4317, name: OpenTelemetryCollectorResource.GRPCEndpointName)
                .WithHttpEndpoint(targetPort: 4318, name: OpenTelemetryCollectorResource.HTTPEndpointName)
                .WithUrlForEndpoint(OpenTelemetryCollectorResource.GRPCEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
                .WithUrlForEndpoint(OpenTelemetryCollectorResource.HTTPEndpointName, u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
                .WithBindMount(otelConfig, "/etc/otelcol-contrib/config.yaml")
                .WithEnvironment("ASPIRE_OTLP_ENDPOINT", $"{dashboardOtlpEndpoint}")
                .WithEnvironment("ASPIRE_API_KEY", builder.Configuration[DashboardOtlpKeyVariableName])
                .WithEnvironment("ASPIRE_INSECURE", "true");

            otel.ApplicationBuilder.Services.TryAddEventingSubscriber<OltpEndpointVariableLifecycle>();

            return otel;
        }
    }
}
