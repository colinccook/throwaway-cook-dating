using CookDating.Conversation.Infrastructure;
using CookDating.Conversation.Worker;
using CookDating.SharedKernel.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// AWS services
builder.Services.AddAwsServices(builder.Configuration);

// Conversation BC services
builder.Services.AddConversationServices();

// Register the SQS consumer as a hosted service
builder.Services.AddHostedService<ConversationEventConsumer>();

var host = builder.Build();
host.Run();
