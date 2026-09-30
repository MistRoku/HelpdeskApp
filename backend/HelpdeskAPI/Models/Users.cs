namespace HelpdeskAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // User, Agent, Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Session revocation: bumped on password change. Tokens carry the version.
    public int SessionVersion { get; set; } = 1;
}

public class PasswordResetToken
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
}
