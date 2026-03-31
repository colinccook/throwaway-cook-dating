using CookDating.Matching.Infrastructure;
using CookDating.Matching.Worker;
using CookDating.SharedKernel.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// AWS services
builder.Services.AddAwsServices(builder.Configuration);

// Matching BC services
builder.Services.AddMatchingServices();

// Register the SQS consumer as a hosted service
builder.Services.AddHostedService<MatchingEventConsumer>();

var host = builder.Build();
host.Run();
