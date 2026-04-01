using Microsoft.AspNetCore.Mvc;

namespace CookDating.Bff.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController(IConfiguration configuration) : ControllerBase
{
    private readonly string _tenantId = configuration["TENANT_ID"] ?? "cook-dating";
    private readonly string _tenantName = configuration["TENANT_NAME"] ?? "Cook Dating";

    [HttpGet]
    public IActionResult GetConfig() =>
        Ok(new { tenantId = _tenantId, tenantName = _tenantName });
}
