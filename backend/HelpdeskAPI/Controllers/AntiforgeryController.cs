using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace HelpdeskAPI.Controllers;

[Route("api/antiforgery")]
[ApiController]
public class AntiforgeryController : ControllerBase
{
    [HttpGet("token")]
    public ActionResult GetToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
