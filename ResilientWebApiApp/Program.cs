using Polly;
using Polly.Timeout;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("my-client")
                .AddStandardResilienceHandler(options =>
                {
                    // Customize retry
                    options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<TimeoutRejectedException>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => response.StatusCode == HttpStatusCode.InternalServerError);
                    options.Retry.MaxRetryAttempts = 5;

                    // Customize attempt timeout
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                });
                //.AddStandardResilienceHandler(options =>
                //{
                //    // Configure standard resilience options here
                //});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/echo", async (IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("my-client");
    var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/comments");
    //var response = await httpClient.GetAsync("https://www.microsoft.dev/");
    return response;
})
.WithName("Echo")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
