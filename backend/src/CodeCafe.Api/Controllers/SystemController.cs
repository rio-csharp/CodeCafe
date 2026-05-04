namespace CodeCafe.Api.Controllers;

using CodeCafe.Contracts.System;
using Microsoft.AspNetCore.Mvc;

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
