using ElectronicHandyman.Api.Controllers;
using ElectronicHandyman.Api.Services;
using ElectronicHandyman.Domain;
using ElectronicHandyman.Scrapper;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
    
    app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapScrapping();

app.Run();
