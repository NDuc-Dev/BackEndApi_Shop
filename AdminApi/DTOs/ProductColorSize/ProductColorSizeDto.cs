using System;

namespace AdminApi.DTOs.ProductColorSize
{
    public class ProductColorSizeDto
    {
        public int ProductColorSizeId { get; set; }
        public int ProductColorId { get; set; }
        public int SizeId { get; set; }
        public int SizeValue {get; set;}
        public int Quantity { get; set; }
    }
}