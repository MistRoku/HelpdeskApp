using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HelpdeskAPI.Models;

namespace HelpdeskAPI.Services;

/// <summary>
/// Minimal JWT style token service using HMAC SHA256. No extra packages required.
/// Access tokens expire in 15 minutes. Refresh tokens are opaque and last 7 days.
/// Passwords use PBKDF2 with per user salt.
/// </summary>
public class TokenService
{
    private readonly byte[] _key;
    private static readonly List<RefreshSession> _refresh = new();
    private static readonly object _lock = new();

    public TokenService(IConfiguration config)
    {
        var secret = config["Auth:JwtSecret"] ?? Environment.GetEnvironmentVariable("HELPDESK_JWT") ?? "dev-secret-change-me-please-rotate-32chars";
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    public string CreateAccessToken(User user)
    {
        var header = Base64Url(JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.Serialize(new
        {
            sub = user.Username,
            role = user.Role,
            sv = user.SessionVersion,
            exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        }));
        var sig = Sign(header + "." + payload);
        return header + "." + payload + "." + sig;
    }

    public string CreateRefreshToken(int userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        lock (_lock) _refresh.Add(new RefreshSession { Token = token, UserId = userId, ExpiresAt = DateTime.UtcNow.AddDays(7) });
        return token;
    }

    public RefreshSession? UseRefreshToken(string token)
    {
        lock (_lock)
        {
            var s = _refresh.FirstOrDefault(r => r.Token == token);
            if (s == null || s.Revoked || s.ExpiresAt < DateTime.UtcNow) return null;
            s.Revoked = true; // rotation
            return s;
        }
    }

    public void RevokeUserRefresh(int userId)
    {
        lock (_lock)
        {
            foreach (var r in _refresh.Where(r => r.UserId == userId)) r.Revoked = true;
        }
    }

    public bool TryValidate(string token, out string username, out string role, out int sessionVersion)
    {
        username = string.Empty; role = string.Empty; sessionVersion = 0;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;
            var expected = Sign(parts[0] + "." + parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(parts[2]))) return false;
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            var doc = JsonDocument.Parse(json);
            var exp = doc.RootElement.GetProperty("exp").GetInt64();
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;
            username = doc.RootElement.GetProperty("sub").GetString() ?? string.Empty;
            role = doc.RootElement.GetProperty("role").GetString() ?? string.Empty;
            sessionVersion = doc.RootElement.GetProperty("sv").GetInt32();
            return !string.IsNullOrWhiteSpace(username);
        }
        catch { return false; }
    }

    private string Sign(string data)
    {
        using var hmac = new HMACSHA256(_key);
        return Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Base64Url(string text) => Base64Url(Encoding.UTF8.GetBytes(text));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace("-", "+").Replace("_", "/");
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }
}
