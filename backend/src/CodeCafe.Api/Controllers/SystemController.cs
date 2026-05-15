using CodeCafe.Contracts.System;

namespace CodeCafe.Api.Controllers;


[ApiController]
[Route("api/system")]
[Tags("System")]
public sealed class SystemController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("info", Name = "GetSystemInfo")]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        return Ok(new SystemInfoResponse(
            "CodeCafe",
            environment.EnvironmentName,
            DateTimeOffset.UtcNow));
    }
}
