using CookDating.Conversation.Worker;
using CookDating.SharedKernel.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
