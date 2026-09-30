using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Data;

public class CatalogStore
{
    private static List<Article> _articles = new()
    {
        new() { Id = 1, Title = "How to reset your password", Content = "Open the sign in page, select Forgot password, follow the emailed link within 60 minutes. Links expire after one use.", Category = "Access", Tags = "password,login,mfa", Status = "Published", Author = "support", ViewCount = 412, HelpfulCount = 96, UnhelpfulCount = 4, PublishedAt = DateTime.UtcNow.AddDays(-30), CreatedAt = DateTime.UtcNow.AddDays(-31), UpdatedAt = DateTime.UtcNow.AddDays(-2) },
        new() { Id = 2, Title = "Network troubleshooting checklist", Content = "Check cable or WiFi, restart router, test VPN, run speed test, log exact error text before opening a ticket.", Category = "Network", Tags = "wifi,vpn,dns", Status = "Published", Author = "support", ViewCount = 287, HelpfulCount = 61, UnhelpfulCount = 9, PublishedAt = DateTime.UtcNow.AddDays(-20), CreatedAt = DateTime.UtcNow.AddDays(-21), UpdatedAt = DateTime.UtcNow.AddDays(-1) },
        new() { Id = 3, Title = "Billing and invoice queries", Content = "Invoices are issued monthly. Reply with invoice number and date for fastest handling.", Category = "Billing", Tags = "invoice,payment", Status = "Published", Author = "finance", ViewCount = 150, HelpfulCount = 40, UnhelpfulCount = 2, PublishedAt = DateTime.UtcNow.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-11), UpdatedAt = DateTime.UtcNow.AddDays(-3) },
    };
    private static int _nextArticleId = 4;

    private static List<CannedResponse> _canned = new()
    {
        new() { Id = 1, Title = "Password reset guide", Content = "Hello {{customer_name}}, here are the steps to reset access for ticket {{ticket_id}}.", Category = "Access", IsShared = true, Owner = "support", UsageCount = 34 },
        new() { Id = 2, Title = "Request received", Content = "Thanks for contacting support. Your ticket {{ticket_id}} is with {{agent_name}}.", Category = "General", IsShared = true, Owner = "support", UsageCount = 58 },
    };
    private static int _nextCannedId = 3;

    private static List<FeedbackEntry> _feedback = new()
    {
        new() { Id = 1, TicketId = 1, Type = "CSAT", Score = 5, Comment = "Fast and clear.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
        new() { Id = 2, TicketId = 2, Type = "CSAT", Score = 4, Comment = "Helpful follow up.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
        new() { Id = 3, TicketId = 1, Type = "NPS", Score = 9, Comment = "Likely to recommend.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
    };
    private static int _nextFeedbackId = 4;

    private static List<SlaBreach> _breaches = new()
    {
        new() { Id = 1, TicketId = 2, BreachType = "Response", BreachedAt = DateTime.UtcNow.AddHours(-2), EscalatedTo = "team-lead" },
    };

    // Articles
    public List<Article> SearchArticles(string? q, string? category)
    {
        var query = _articles.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => (a.Title + " " + a.Content + " " + a.Tags).Contains(q, StringComparison.OrdinalIgnoreCase));
        return query.OrderByDescending(a => a.ViewCount).ToList();
    }

    public Article? GetArticle(int id) => _articles.FirstOrDefault(a => a.Id == id);

    public Article CreateArticle(Article a, string author)
    {
        a.Id = _nextArticleId++;
        a.Title = SanitizerService.Sanitize(a.Title, 200);
        a.Content = SanitizerService.Sanitize(a.Content, 50000);
        a.Author = author;
        a.CreatedAt = DateTime.UtcNow;
        a.UpdatedAt = DateTime.UtcNow;
        if (a.Status == "Published") a.PublishedAt = DateTime.UtcNow;
        _articles.Add(a);
        return a;
    }

    public bool UpdateArticle(int id, Article updated)
    {
        var existing = _articles.FirstOrDefault(a => a.Id == id);
        if (existing == null) return false;
        existing.Title = SanitizerService.Sanitize(updated.Title, 200);
        existing.Content = SanitizerService.Sanitize(updated.Content, 50000);
        existing.Category = updated.Category;
        existing.Tags = SanitizerService.Sanitize(updated.Tags, 500);
        existing.Status = updated.Status;
        existing.Version += 1;
        existing.UpdatedAt = DateTime.UtcNow;
        if (existing.Status == "Published") existing.PublishedAt ??= DateTime.UtcNow;
        return true;
    }

    public bool DeleteArticle(int id)
    {
        var a = _articles.FirstOrDefault(x => x.Id == id);
        if (a == null) return false;
        _articles.Remove(a);
        return true;
    }

    public void RecordView(int id) { var a = GetArticle(id); if (a != null) a.ViewCount++; }
    public void RecordFeedback(int id, bool helpful) { var a = GetArticle(id); if (a == null) return; if (helpful) a.HelpfulCount++; else a.UnhelpfulCount++; }

    // Canned
    public List<CannedResponse> GetCanned() => _canned.OrderByDescending(c => c.UsageCount).ToList();
    public CannedResponse CreateCanned(CannedResponse c, string owner)
    {
        c.Id = _nextCannedId++;
        c.Title = SanitizerService.Sanitize(c.Title, 100);
        c.Content = SanitizerService.Sanitize(c.Content, 5000);
        c.Owner = owner;
        _canned.Add(c);
        return c;
    }
    public void TrackCannedUse(int id) { var c = _canned.FirstOrDefault(x => x.Id == id); if (c != null) c.UsageCount++; }

    // Feedback
    public FeedbackEntry AddFeedback(FeedbackEntry f)
    {
        f.Id = _nextFeedbackId++;
        f.Comment = SanitizerService.Sanitize(f.Comment, 500);
        _feedback.Add(f);
        return f;
    }
    public List<FeedbackEntry> GetFeedback() => _feedback.OrderByDescending(f => f.CreatedAt).ToList();
    public object GetFeedbackStats()
    {
        var csat = _feedback.Where(f => f.Type == "CSAT").ToList();
        var nps = _feedback.Where(f => f.Type == "NPS").ToList();
        double avgCsat = csat.Count == 0 ? 0 : Math.Round(csat.Average(c => c.Score), 2);
        double avgNps = nps.Count == 0 ? 0 : Math.Round(nps.Average(c => c.Score), 2);
        int promoters = nps.Count(c => c.Score >= 9);
        int detractors = nps.Count(c => c.Score <= 6);
        int npsScore = nps.Count == 0 ? 0 : (int)Math.Round((promoters - detractors) * 100.0 / nps.Count);
        return new { csatAverage = avgCsat, csatCount = csat.Count, npsAverage = avgNps, npsScore, npsCount = nps.Count };
    }

    public List<SlaBreach> GetBreaches() => _breaches;
}
