using System.ComponentModel.DataAnnotations;

namespace HelpdeskAPI.DTOs;

public class CreateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(10000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [RegularExpression("^(Low|Medium|High|Urgent)$")]
    public string Priority { get; set; } = "Medium";

    [RegularExpression("^(Network|Software|Hardware|Access|Billing|Other|General)$")]
    public string Category { get; set; } = "General";

    [RegularExpression("^(Web|Email|Chat|Phone)$")]
    public string Channel { get; set; } = "Web";
}

public class UpdateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(10000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [RegularExpression("^(Low|Medium|High|Urgent)$")]
    public string Priority { get; set; } = "Medium";

    [RegularExpression("^(Network|Software|Hardware|Access|Billing|Other|General)$")]
    public string Category { get; set; } = "General";

    public bool AiOverridden { get; set; }
}

public class StatusRequest
{
    [Required, RegularExpression("^(New|Open|InProgress|Pending|Resolved|Closed)$")]
    public string Status { get; set; } = string.Empty;
}

public class AssignRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Assignee { get; set; } = string.Empty;
}

public class ReplyRequest
{
    [Required, StringLength(10000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
