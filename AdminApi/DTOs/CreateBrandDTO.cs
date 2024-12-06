using System.ComponentModel.DataAnnotations;

namespace AdminApi.DTOs
{
#nullable disable
    public class CreateBrandDTO
    {
        [Required(ErrorMessage = "Brand name is required")]
        public string BrandName { get; set; }
        [Required(ErrorMessage = "Brand descriptions is required")]
        public string Descriptions { get; set; }
        [Required(ErrorMessage = "Image is required")]
        public IFormFile Image { get; set; }
    }
}