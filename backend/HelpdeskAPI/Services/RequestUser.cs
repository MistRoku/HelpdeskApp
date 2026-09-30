using System.Security.Claims;

namespace HelpdeskAPI.Services;

public record RequestUser(string Username, string Role);

public static class RequestUserHelper
{
    public static RequestUser Current(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var name = ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? ctx.User.Identity.Name ?? string.Empty;
            var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            return new RequestUser(name, role);
        }
        // Dev fallback for seed data and offline frontend
        var hUser = ctx.Request.Headers["X-User"].ToString();
        var hRole = ctx.Request.Headers["X-Role"].ToString();
        if (!string.IsNullOrWhiteSpace(hUser))
            return new RequestUser(hUser, string.IsNullOrWhiteSpace(hRole) ? "Agent" : hRole);
        return new RequestUser("anonymous", "User");
    }

    public static bool IsAgentOrAdmin(RequestUser u) => u.Role == "Agent" || u.Role == "Admin";
    public static bool IsAdmin(RequestUser u) => u.Role == "Admin";
}
