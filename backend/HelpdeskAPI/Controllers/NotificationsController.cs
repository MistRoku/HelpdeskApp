using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/notifications")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notifications;
    public NotificationsController(NotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public ActionResult List()
    {
        var me = RequestUserHelper.Current(HttpContext);
        if (me.Username == "anonymous") return Ok(new object[0]);
        return Ok(_notifications.ForUser(me.Username));
    }

    [HttpPost("{id}/read")]
    [ValidateAntiForgeryToken]
    public ActionResult MarkRead(int id)
    {
        var me = RequestUserHelper.Current(HttpContext);
        return _notifications.MarkRead(id, me.Username) ? Ok() : NotFound();
    }
}
