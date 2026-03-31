var builder = DistributedApplication.CreateBuilder(args);

var floci = builder.AddContainer("floci", "hectorvent/floci", "latest")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "default", scheme: "http")
    .WithLifetime(ContainerLifetime.Session);

var flociEndpoint = floci.GetEndpoint("default");

var bff = builder.AddProject<Projects.CookDating_Bff>("bff")
    .WaitFor(floci)
    .WithEnvironment("AWS__ServiceURL", flociEndpoint)
    .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
    .WithEnvironment("AWS__AccessKey", "test")
    .WithEnvironment("AWS__SecretKey", "test")
    .WithEnvironment("ClientApp__Url", "http://localhost:5173")
    .WithExternalHttpEndpoints();

var clientApp = builder.AddViteApp("client-app", "../client-app")
    .WithExternalHttpEndpoints()
    .WithReference(bff)
    .WaitFor(bff);

var matchingWorker = builder.AddProject<Projects.CookDating_Matching_Worker>("matching-worker")
    .WaitFor(floci)
    .WithEnvironment("AWS__ServiceURL", flociEndpoint)
    .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
    .WithEnvironment("AWS__AccessKey", "test")
    .WithEnvironment("AWS__SecretKey", "test");

var conversationWorker = builder.AddProject<Projects.CookDating_Conversation_Worker>("conversation-worker")
    .WaitFor(floci)
    .WithEnvironment("AWS__ServiceURL", flociEndpoint)
    .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
    .WithEnvironment("AWS__AccessKey", "test")
    .WithEnvironment("AWS__SecretKey", "test");

builder.Build().Run();
