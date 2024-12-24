using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AdminApi.DTOs.Brand;
using AdminApi.DTOs.NameTag;
using AdminApi.DTOs.ProductColor;

namespace AdminApi.DTOs.Product
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Product name is required !")]
        public string? ProductName { get; set; }
        public string? ImagePath { get; set; }
        [Required(ErrorMessage = "Descriptions is required !")]
        public string? ProductDescription { get; set; }
        public int BrandId { get; set; }
        public bool Status { get; set; }
    }
}
