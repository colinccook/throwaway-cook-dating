using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CookDating.SharedKernel.Infrastructure;

public static class AwsServiceRegistration
{
    /// <summary>
    /// Registers IAmazonDynamoDB, IAmazonSimpleNotificationService, and IAmazonSQS
    /// as singletons configured from the AWS section of the app configuration.
    /// </summary>
    public static IServiceCollection AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceUrl = configuration["AWS:ServiceURL"];
        var region = configuration["AWS:AuthenticationRegion"] ?? "us-east-1";
        var accessKey = configuration["AWS:AccessKey"] ?? "test";
        var secretKey = configuration["AWS:SecretKey"] ?? "test";

        var credentials = new BasicAWSCredentials(accessKey, secretKey);

        services.AddSingleton<IAmazonDynamoDB>(_ =>
            new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            }));

        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
            new AmazonSimpleNotificationServiceClient(credentials, new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            }));

        services.AddSingleton<IAmazonSQS>(_ =>
            new AmazonSQSClient(credentials, new AmazonSQSConfig
            {
                ServiceURL = serviceUrl,
                AuthenticationRegion = region
            }));

        return services;
    }

    /// <summary>
    /// Registers the AWS bootstrapper as a hosted service that creates DynamoDB tables,
    /// SNS topics, and SQS queues on startup.
    /// </summary>
    public static IServiceCollection AddAwsBootstrapper(this IServiceCollection services)
    {
        services.AddHostedService<AwsBootstrapHostedService>();
        return services;
    }
}
