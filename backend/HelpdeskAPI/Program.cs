using System.Security.Claims;
using HelpdeskAPI.Data;
using HelpdeskAPI.Hubs;
using HelpdeskAPI.Services;

namespace HelpdeskAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR();

        builder.Services.AddSingleton<ITicketRepository, TicketRepository>();
        builder.Services.AddSingleton<CatalogStore>();
        builder.Services.AddSingleton<AiClassificationService>();
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddHostedService<AutoCloseWorker>();

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-CSRF";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "__Host-Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.IdleTimeout = TimeSpan.FromMinutes(30);
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("password-reset", o =>
            {
                o.PermitLimit = 3;
                o.Window = TimeSpan.FromHours(1);
                o.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("api", o =>
            {
                o.PermitLimit = 200;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });
            options.RejectionStatusCode = 429;
        });

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("HelpdeskClient", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN", "X-User", "X-Role", "X-Allow-Write")
                      .AllowCredentials();
            });
        });

        builder.Services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseCors("HelpdeskClient");
        app.UseRateLimiter();
        app.UseSession();

        // Bearer token auth. Validates HMAC token, checks session version for revocation.
        app.Use(async (context, next) =>
        {
            var header = context.Request.Headers["Authorization"].ToString();
            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = header.Substring("Bearer ".Length).Trim();
                var svc = context.RequestServices.GetRequiredService<TokenService>();
                if (svc.TryValidate(token, out var username, out var role, out var sv))
                {
                    var user = Controllers.AuthController.FindByName(username);
                    if (user != null && user.SessionVersion == sv)
                    {
                        var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role) };
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                    }
                }
            }
            await next();
        });

        app.UseAuthorization();
        app.MapControllers().RequireRateLimiting("api");
        app.MapHub<TicketHub>("/hubs/tickets");

        app.Run();
    }
}

/// <summary>
/// Closes tickets idle for 14 days. Runs hourly.
/// </summary>
public class AutoCloseWorker : BackgroundService
{
    private readonly IServiceProvider _provider;
    public AutoCloseWorker(IServiceProvider provider) => _provider = provider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                await repo.AutoCloseIdleAsync();
            }
            catch { }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
