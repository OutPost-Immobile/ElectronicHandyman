using ElectronicHandyman.Api.Controllers;
using ElectronicHandyman.Api.Services;
using ElectronicHandyman.Domain;
using ElectronicHandyman.Scrapper;
using Scalar.AspNetCore;
using Serilog;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAntiforgery();

builder.Services.AddSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddScrapper(builder.Configuration);
builder.Services.AddDomain(builder.Configuration);

builder.Services.AddScoped<ArduinoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.MapScalarApiReference();
    app.MapScalarApiReference();
    
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapScrapping();

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
    .WithName("GetWeatherForecast");

app.MapPost("/api/image/upload", async (IFormFile file) =>
    {

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var imageBytes = memoryStream.ToArray();
        
        ImageProcessing.ProcessImage(imageBytes);
        return TypedResults.Ok("Plik wczytany pomyślnie");
    })
    .WithName("UploadImage")
    .DisableAntiforgery();
app.Run();
