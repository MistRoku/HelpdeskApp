using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    [HttpGet]
    public ActionResult List()
    {
        if (!RequestUserHelper.IsAdmin(RequestUserHelper.Current(HttpContext))) return Forbid();
        return Ok(AuthController.AllUsers().Select(u => new { u.Id, u.Username, u.DisplayName, u.Email, u.Role, u.EmailVerified, u.CreatedAt }));
    }

    [HttpPut("{username}/role")]
    [ValidateAntiForgeryToken]
    public ActionResult SetRole(string username, [FromBody] RoleBody body)
    {
        if (!RequestUserHelper.IsAdmin(RequestUserHelper.Current(HttpContext))) return Forbid();
        if (body.Role != "User" && body.Role != "Agent" && body.Role != "Admin") return BadRequest("Role must be User, Agent, or Admin.");
        var user = AuthController.FindByName(username);
        if (user == null) return NotFound();
        user.Role = body.Role;
        user.SessionVersion += 1;
        return Ok(new { message = "Role updated." });
    }

    [HttpDelete("{username}")]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(string username)
    {
        if (!RequestUserHelper.IsAdmin(RequestUserHelper.Current(HttpContext))) return Forbid();
        var user = AuthController.FindByName(username);
        if (user == null) return NotFound();
        AuthController.AllUsers().Remove(user);
        return NoContent();
    }

    public record RoleBody(string Role);
}
