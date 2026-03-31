using CookDating.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddAwsBootstrapper();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }))
    .WithName("HealthCheck");

app.Run();
