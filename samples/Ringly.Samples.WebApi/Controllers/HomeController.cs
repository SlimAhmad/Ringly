using Microsoft.AspNetCore.Mvc;

namespace Ringly.Samples.WebApi.Controllers;

// Unauthenticated heartbeat every system should have — confirms the app is up, nothing more.
[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        this.Ok(new { Status = "Healthy" });
}
