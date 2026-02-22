using System.ComponentModel.DataAnnotations;

namespace PlechPomoshchi.ViewModels;

public class VolunteerOrgApplicationVm
{
    [Required(ErrorMessage = "Введите название организации")]
    public string OrgName { get; set; } = "";

    public string? Website { get; set; }

    [Required(ErrorMessage = "Введите контактное лицо")]
    public string ContactName { get; set; } = "";

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string ContactEmail { get; set; } = "";

    public string? ContactPhone { get; set; }

    [Required(ErrorMessage = "Введите сообщение")]
    [MinLength(10, ErrorMessage = "Слишком коротко")]
    public string Message { get; set; } = "";
}
