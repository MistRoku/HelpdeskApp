using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HelpdeskAPI.DTOs;
using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private static List<User> _users = new()
    {
        new() { Id = 1, Username = "admin", DisplayName = "Admin", Email = "admin@example.com", EmailVerified = true, PasswordHash = TokenService.HashPassword("Adminpass123"), Role = "Admin" },
        new() { Id = 2, Username = "agent1", DisplayName = "Agent One", Email = "agent@example.com", EmailVerified = true, PasswordHash = TokenService.HashPassword("Agentpass123"), Role = "Agent" },
        new() { Id = 3, Username = "john.doe", DisplayName = "John Doe", Email = "john@example.com", EmailVerified = true, PasswordHash = TokenService.HashPassword("Userpass123"), Role = "User" },
    };
    private static int _nextUserId = 4;

    private static List<PasswordResetToken> _resets = new();
    private static Dictionary<string, (int Count, DateTime WindowStart)> _resetAttempts = new();
    private static readonly object ResetLock = new();

    private readonly TokenService _tokens;
    private readonly NotificationService _notifications;

    public AuthController(TokenService tokens, NotificationService notifications)
    {
        _tokens = tokens;
        _notifications = notifications;
    }

    public record RefreshRequest(string RefreshToken);
    public record RequestResetRequest(string Email);
    public record ConfirmResetRequest(string Token, string NewPassword);
    public record ChangePasswordRequest(string Username, string NewPassword);
    public record RoleRequest(string Role);

    public static User? FindByName(string username) =>
        _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    public static List<User> AllUsers() => _users;

    private object Shape(User u) => new { u.Id, u.Username, u.DisplayName, u.Email, u.Role, u.EmailVerified, u.CreatedAt };

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public ActionResult Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Username, email, and password are required.");
        if (req.Password.Length < 10) return BadRequest("Password must be at least 10 characters.");
        if (FindByName(req.Username) != null) return Conflict("Username already exists.");
        if (_users.Any(u => u.Email.Equals(req.Email, StringComparison.OrdinalIgnoreCase))) return Conflict("Email already registered.");

        var user = new User
        {
            Id = _nextUserId++,
            Username = SanitizerService.Sanitize(req.Username, 100),
            DisplayName = SanitizerService.Sanitize(req.DisplayName, 100),
            Email = SanitizerService.Sanitize(req.Email, 200),
            PasswordHash = TokenService.HashPassword(req.Password),
            Role = "User",
            EmailVerified = false // verification email sent in production
        };
        _users.Add(user);
        _notifications.Audit(null, user.Username, "Registered account");
        return Ok(new { message = "Registered. Verification email sent.", user = Shape(user) });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public ActionResult Login([FromBody] LoginRequest req)
    {
        var user = FindByName(req.Username);
        if (user == null || !TokenService.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials." });
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken(user.Id);
        return Ok(new { accessToken = access, refreshToken = refresh, user = Shape(user), expiresInMinutes = 15 });
    }

    [HttpPost("refresh")]
    public ActionResult Refresh([FromBody] RefreshRequest req)
    {
        var session = _tokens.UseRefreshToken(req.RefreshToken);
        if (session == null) return Unauthorized(new { error = "Refresh token invalid or expired." });
        var user = _users.FirstOrDefault(u => u.Id == session.UserId);
        if (user == null) return Unauthorized(new { error = "Refresh token invalid or expired." });
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken(user.Id);
        return Ok(new { accessToken = access, refreshToken = refresh, expiresInMinutes = 15 });
    }

    [HttpPost("request-reset")]
    [EnableRateLimiting("password-reset")]
    public ActionResult RequestReset([FromBody] RequestResetRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        lock (ResetLock)
        {
            var now = DateTime.UtcNow;
            if (!_resetAttempts.TryGetValue(ip, out var entry) || (now - entry.WindowStart).TotalHours >= 1)
                _resetAttempts[ip] = (1, now);
            else if (entry.Count >= 3)
                return StatusCode(429, new { error = "Too many reset attempts. Try again later." });
            else
                _resetAttempts[ip] = (entry.Count + 1, entry.WindowStart);
        }

        var email = SanitizerService.Sanitize(req.Email, 200);
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user == null) return Ok(new { message = "If the address exists, a reset link was sent and expires in 60 minutes." });

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        _resets.Add(new PasswordResetToken { Token = token, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddMinutes(60), Used = false });
        return Ok(new { message = "If the address exists, a reset link was sent and expires in 60 minutes.", expiresInMinutes = 60 });
    }

    [HttpPost("confirm-reset")]
    [EnableRateLimiting("password-reset")]
    public ActionResult ConfirmReset([FromBody] ConfirmResetRequest req)
    {
        var entry = _resets.FirstOrDefault(r => r.Token == req.Token);
        if (entry == null || entry.Used || entry.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "Reset link is invalid or expired." });
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 10)
            return BadRequest(new { error = "Password must be at least 10 characters." });

        var user = _users.FirstOrDefault(u => u.Id == entry.UserId);
        if (user == null) return BadRequest(new { error = "Reset link is invalid or expired." });

        user.PasswordHash = TokenService.HashPassword(req.NewPassword);
        user.SessionVersion += 1;
        _tokens.RevokeUserRefresh(user.Id);
        entry.Used = true;
        return Ok(new { message = "Password updated. All other sessions were signed out." });
    }

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public ActionResult ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var me = RequestUserHelper.Current(HttpContext);
        var user = FindByName(req.Username) ?? FindByName(me.Username);
        if (user == null) return NotFound();
        if (me.Username != user.Username && me.Role != "Admin") return Forbid();
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 10)
            return BadRequest(new { error = "Password must be at least 10 characters." });
        user.PasswordHash = TokenService.HashPassword(req.NewPassword);
        user.SessionVersion += 1;
        _tokens.RevokeUserRefresh(user.Id);
        return Ok(new { message = "Password changed. All other sessions were signed out." });
    }

    [HttpGet("me")]
    public ActionResult Me()
    {
        var me = RequestUserHelper.Current(HttpContext);
        var user = FindByName(me.Username);
        if (user == null) return Unauthorized();
        return Ok(Shape(user));
    }
}
