using CookDating.Bff.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CookDating.UnitTests.Infrastructure;

[TestFixture]
public class ConfigControllerTests
{
    [Test]
    public void GetConfig_ReturnsConfiguredTenantValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TENANT_ID"] = "tech-dating",
                ["TENANT_NAME"] = "Tech Dating"
            })
            .Build();

        var controller = new ConfigController(config);
        var result = controller.GetConfig() as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("tech-dating"));
            Assert.That(json, Does.Contain("Tech Dating"));
        });
    }

    [Test]
    public void GetConfig_ReturnsDefaults_WhenNoConfiguration()
    {
        var config = new ConfigurationBuilder().Build();
        var controller = new ConfigController(config);
        var result = controller.GetConfig() as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("cook-dating"));
            Assert.That(json, Does.Contain("Cook Dating"));
        });
    }
}
