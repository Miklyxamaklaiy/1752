using System.ComponentModel.DataAnnotations;

namespace PlechPomoshchi.Models;

public class VolunteerOrgApplication
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string OrgName { get; set; } = "";

    [MaxLength(250)]
    public string? Website { get; set; }

    [MaxLength(250)]
    public string ContactName { get; set; } = "";

    [EmailAddress]
    public string ContactEmail { get; set; } = "";

    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    public string Message { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
