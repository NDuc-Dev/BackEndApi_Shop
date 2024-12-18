using System.ComponentModel.DataAnnotations;
using AdminApi.DTOs.NameTag;
using AdminApi.DTOs.ProductColor;

namespace AdminApi.DTOs.Product
{
#nullable disable
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product id is required")]
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Product Name is required !")]
        public string ProductName { get; set; }
        [Required(ErrorMessage = "Descriptions is required !")]
        public string Descriptions { get; set; }
        public int BrandId { get; set; }
        public bool Status { get; set;}
        public List<NameTagDto> NameTag { get; set; }
        public List<ProductColorDto> Variant { get; set; }
    }
}