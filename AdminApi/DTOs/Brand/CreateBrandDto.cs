using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AdminApi.DTOs.Brand
{
    #nullable disable
    public class CreateBrandDto
    {
        [Required(ErrorMessage = "Brand name is required")]
        public string BrandName { get; set; }
        [Required(ErrorMessage = "Brand descriptions is required")]
        public string Descriptions { get; set; }
        [Required(ErrorMessage = "Image is required")]
        public IFormFile Image { get; set; }
    }
}
