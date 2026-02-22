using System.ComponentModel.DataAnnotations;

namespace PlechPomoshchi.Models;

public class AppUser
{
    public int Id { get; set; }

    [EmailAddress]
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(50)]
    public string? Phone { get; set; }

    // Admin, Requester, Volunteer
    [MaxLength(30)]
    public string Role { get; set; } = "Requester";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
