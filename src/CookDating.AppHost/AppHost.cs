var builder = DistributedApplication.CreateBuilder(args);

var useContainerImages = Environment.GetEnvironmentVariable("USE_CONTAINER_IMAGES") == "true";

// Tenant configuration — defaults to single tenant
var tenantsSection = builder.Configuration.GetSection("Tenants");
var tenants = tenantsSection.GetChildren().Select(c => c.Value!).ToArray();
if (tenants.Length == 0) tenants = ["cook-dating"];

static string ToDisplayName(string tenantId) =>
    string.Join(' ', tenantId.Split('-').Select(w =>
        string.Concat(char.ToUpperInvariant(w[0]).ToString(), w.Substring(1))));

var floci = builder.AddContainer("floci", "hectorvent/floci", "latest")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "default", scheme: "http")
    .WithLifetime(ContainerLifetime.Session);

var flociEndpoint = floci.GetEndpoint("default");

if (useContainerImages)
{
    // Use a dotless network alias so floci doesn't parse the hostname as an S3 bucket
    floci.WithContainerNetworkAlias("awslocal");
    const string flociContainerUrl = "http://awslocal:4566";

    // Per-tenant BFF + client-app
    Aspire.Hosting.ApplicationModel.IResourceBuilder<Aspire.Hosting.ApplicationModel.ContainerResource>? firstBff = null;
    foreach (var tenantId in tenants)
    {
        var bff = builder.AddContainer($"{tenantId}-bff", "cookdating-bff")
            .WithEndpoint(targetPort: 8080, name: "http", scheme: "http")
            .WithHttpHealthCheck("/health")
            .WaitFor(floci)
            .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
            .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
            .WithEnvironment("AWS__AccessKey", "test")
            .WithEnvironment("AWS__SecretKey", "test")
            .WithEnvironment("TENANT_ID", tenantId)
            .WithEnvironment("TENANT_NAME", ToDisplayName(tenantId))
            .WithExternalHttpEndpoints();

        var bffEndpoint = bff.GetEndpoint("http");

        var clientApp = builder.AddContainer($"{tenantId}-client", "cookdating-client-app")
            .WithEndpoint(targetPort: 80, name: "http", scheme: "http")
            .WithHttpHealthCheck("/")
            .WaitFor(bff)
            .WithExternalHttpEndpoints();

        clientApp.WithEnvironment("BFF_URL", bffEndpoint);
        bff.WithEnvironment("ClientApp__Url", clientApp.GetEndpoint("http"));

        firstBff ??= bff;
    }

    // Single shared worker pair — waits for first BFF (which bootstraps AWS resources)
    builder.AddContainer("matching-worker", "cookdating-matching-worker")
        .WaitFor(floci)
        .WaitFor(firstBff!)
        .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");

    builder.AddContainer("conversation-worker", "cookdating-conversation-worker")
        .WaitFor(floci)
        .WaitFor(firstBff!)
        .WithEnvironment("AWS__ServiceURL", flociContainerUrl)
        .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
        .WithEnvironment("AWS__AccessKey", "test")
        .WithEnvironment("AWS__SecretKey", "test");
}
else
{
    // Per-tenant BFF + client-app
    foreach (var tenantId in tenants)
    {
        var bff = builder.AddProject<Projects.CookDating_Bff>($"{tenantId}-bff")
            .WaitFor(floci)
            .WithEnvironment("AWS__ServiceURL", flociEndpoint)
            .WithEnvironment("AWS__AuthenticationRegion", "us-east-1")
            .WithEnvironment("AWS__AccessKey", "test")
            .WithEnvironment("AWS__SecretKey", "test")
            .WithEnvironment("TENANT_ID", tenantId)
            .WithEnvironment("TENANT_NAME", ToDisplayName(tenantId))
            .WithEnvironment("ClientApp__Url", "http://localhost:5173")
            .WithExternalHttpEndpoints();

        builder.AddViteApp($"{tenantId}-client", "../client-app")
            .WithExternalHttpEndpoints()
            .WithReference(bff)
            .WaitFor(bff);
    }

    // Single shared worker pair
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
