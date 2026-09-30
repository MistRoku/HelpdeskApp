using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

/// <summary>
/// Admin audit trail of ticket and account actions.
/// </summary>
[Route("api/audit")]
[ApiController]
public class AuditController : ControllerBase
{
    private readonly NotificationService _notifications;
    public AuditController(NotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public ActionResult List([FromQuery] int take = 50)
    {
        if (!RequestUserHelper.IsAdmin(RequestUserHelper.Current(HttpContext))) return Forbid();
        return Ok(_notifications.Recent(Math.Min(take, 200)));
    }
}
