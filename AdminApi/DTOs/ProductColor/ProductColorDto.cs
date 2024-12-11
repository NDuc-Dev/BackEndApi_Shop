using AdminApi.DTOs.ProductColorSize;

namespace AdminApi.DTOs.ProductColor
{
    public class ProductColorDto
    {
        public int ProductColorId { get; set; }
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public decimal UnitPrice { get; set; }
        public List<string>? ImagePath { get; set; }
        public List<ProductColorSizeDto>? ProductColorSize { get; set; }
    }
}