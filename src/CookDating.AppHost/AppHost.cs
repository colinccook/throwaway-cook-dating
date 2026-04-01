var builder = DistributedApplication.CreateBuilder(args);

var useContainerImages = Environment.GetEnvironmentVariable("USE_CONTAINER_IMAGES") == "true";

var floci = builder.AddContainer("floci", "hectorvent/floci", "latest")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "default", scheme: "http")
    .WithLifetime(ContainerLifetime.Session);

var flociEndpoint = floci.GetEndpoint("default");

if (useContainerImages)
{
    // Use a dotless network alias so floci doesn't parse the hostname as an S3 bucket
    floci.WithContainerNetworkAlias("awslocal");
    const string flociContainerUrl = "http://awslocal:4566";

    var bff = builder.AddContainer("bff", "cookdating-bff")
        .WithEndpoint(targetPort: 8080, name: "http", scheme: "http")
        .WithHttpHealthCheck("/health")
        .WaitFor(floci)
        .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test")
        .WithExternalHttpEndpoints();

    var clientApp = builder.AddContainer("client-app", "cookdating-client-app")
        .WithEndpoint(targetPort: 80, name: "http", scheme: "http")
        .WithHttpHealthCheck("/")
        .WaitFor(bff)
        .WithEnvironment("BFF_URL", bff.GetEndpoint("http"))
        .WithExternalHttpEndpoints();

    bff.WithEnvironment("ClientApp__Url", clientApp.GetEndpoint("http"));

    builder.AddContainer("matching-worker", "cookdating-matching-worker")
        .WaitFor(floci)
        .WaitFor(bff)
        .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");

    builder.AddContainer("conversation-worker", "cookdating-conversation-worker")
        .WaitFor(floci)
        .WaitFor(bff)
        .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");
}
else
{
    var bff = builder.AddProject<Projects.CookDating_Bff>("bff")
        .WaitFor(floci)
        .WithEnvironment("AWS__ServiceURL", flociEndpoint)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test")
        .WithEnvironment("ClientApp__Url", "http://localhost:5173")
        .WithExternalHttpEndpoints();

    builder.AddViteApp("client-app", "../client-app")
        .WithExternalHttpEndpoints()
        .WithReference(bff)
        .WaitFor(bff);

    builder.AddProject<Projects.CookDating_Matching_Worker>("matching-worker")
        .WaitFor(floci)
        .WithEnvironment("AWS__ServiceURL", flociEndpoint)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");

    builder.AddProject<Projects.CookDating_Conversation_Worker>("conversation-worker")
        .WaitFor(floci)
        .WithEnvironment("AWS__ServiceURL", flociEndpoint)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");
}

builder.Build().Run();
