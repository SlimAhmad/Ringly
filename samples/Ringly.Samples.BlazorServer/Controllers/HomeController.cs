using Microsoft.AspNetCore.Mvc;

namespace Ringly.Samples.BlazorServer.Controllers;

// Unauthenticated heartbeat endpoint — per the-standard-architecture's REST rules, every system
// should have one, indicating aliveness only, no security required.
[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => this.Ok();
}
