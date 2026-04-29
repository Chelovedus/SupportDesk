using Microsoft.AspNetCore.Mvc;

namespace SupportDesk.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "SupportDesk.Api"
        });
    }
}