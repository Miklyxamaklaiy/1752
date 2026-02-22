using System.ComponentModel.DataAnnotations;

namespace PlechPomoshchi.Models;

public class Organization
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string Name { get; set; } = "";

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(120)]
    public string Category { get; set; } = "Помощь";

    [MaxLength(500)]
    public string? Website { get; set; }

    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public bool IsFromParser { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
