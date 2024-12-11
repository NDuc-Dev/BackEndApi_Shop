using System.ComponentModel.DataAnnotations;

namespace AdminApi.DTOs.NameTag
{
    public class CreateNameTagDto
    {
        [Required(ErrorMessage = "Tag Name is required")]
        public string? TagName { get; set; }
    }
}