using System.ComponentModel.DataAnnotations;

namespace AdminApi.DTOs.Size
{
    public class CreateSizeDto
    {
        [Required(ErrorMessage = "Size value is required !")]
        [Range(1, 100, ErrorMessage = "Size vlaue is invalid")]
        public int SizeValue { get; set; }
    }
}