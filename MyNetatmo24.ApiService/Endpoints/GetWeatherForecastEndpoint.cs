namespace MyNetatmo24.ApiService.Endpoints;

public static class GetWeatherForecastEndpoint
{
    public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    public class Endpoint : Ep.NoReq.Res<IEnumerable<WeatherForecast>>
    {
        public override void Configure()
        {
            Get("/weatherforecast");
            Policies(Constants.Policies.ReadWeather);
            Description(d =>
                d.Produces<IEnumerable<WeatherForecast>>()
                    .WithName("GetWeatherForecast"));
            Summary(s =>
            {
                s.Summary = "Returns the weather forecast";
                s.Description = "Returns the weather forecast for the next 5 days";
                s.Responses[StatusCodes.Status200OK] = "The weather forecast is returned";
            });
        }

        public override async Task HandleAsync(CancellationToken ct)
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
            await Send.OkAsync(forecast, ct);
        }
    }
}
