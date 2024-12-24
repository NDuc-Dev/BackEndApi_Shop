using System;

namespace AdminApi.DTOs.ProductColorSize
{
    public class ProductColorSizeDto
    {
        public int? ProductColorSizeId { get; set; } = null;
        public int ProductColorId { get; set; }
        public int SizeId { get; set; }
        public int Quantity { get; set; }
    }
}