using System.Collections.Generic;
using AdminApi.DTOs.Brand;
using AdminApi.DTOs.NameTag;
using AdminApi.DTOs.ProductColor;

namespace AdminApi.DTOs.Product
{
    #nullable disable
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public BrandDto Brand { get; set; }
        public bool Status { get; set; }
        public List<NameTagDto> Tag { get; set; }
        public List<ProductColorDto> Variant { get; set; }
    }
}
