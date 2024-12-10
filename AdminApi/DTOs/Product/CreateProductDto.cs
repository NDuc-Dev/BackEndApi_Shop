using System.ComponentModel.DataAnnotations;
using AdminApi.DTOs.ProductColor;

namespace AdminApi.DTOs.Product
{
    #nullable disable
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product Name is required")]
        public string ProductName { get; set; }
        [Required(ErrorMessage = "Product Description is required")]
        public string ProductDescription { get; set; }
        public int BrandId { get; set; }
        public List<int> NameTagId { get; set; }
        public List<CreateProductColorDto> Variants { get; set; }
    }
}
