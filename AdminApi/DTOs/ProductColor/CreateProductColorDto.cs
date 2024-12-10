
using AdminApi.DTOs.ProductColorSize;

namespace AdminApi.DTOs.ProductColor
{
    #nullable disable
    public class CreateProductColorDto
    {
        public int ColorId { get; set; }
        public decimal Price { get; set; }
        public List<string> images { get; set; }
        public List<CreateProductColorSizeDto> ProductColorSize { get; set; }
    }
}