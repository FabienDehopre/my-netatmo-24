using MartinCostello.OpenApi;

namespace MyNetatmo24.ApiService.Endpoints;

public sealed class GetWeatherForecastEndpoint : SharedKernel.Endpoints.EndpointWithoutRequest<IEnumerable<GetWeatherForecastEndpoint.WeatherForecast>>
{
    public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    public override void Configure(IEndpointRouteBuilder builder)
    {
        builder
            .MapGet("weatherforecast", InvokeAsync)
            .WithName("GetWeatherForecast")
            .WithSummary("Gets the weather forecast.")
            .WithDescription("Retrieves the weather forecast for the next 5 days.")
            .RequireAuthorization(Constants.Policies.ReadWeather)
            .Produces<IEnumerable<WeatherForecast>>()
            .ProducesOpenApiResponse(StatusCodes.Status200OK, "The weather forecast was successfully retrieved.");
    }

    public override Task<IEnumerable<WeatherForecast>> InvokeAsync(CancellationToken ct)
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
#pragma warning disable CA5394
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
#pragma warning restore CA5394
                ))
            .ToArray();
        return Task.FromResult<IEnumerable<WeatherForecast>>(forecast);
    }
}
