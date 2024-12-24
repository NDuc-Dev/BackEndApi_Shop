namespace AdminApi.DTOs.Product
{
#nullable disable
    public class ProductListDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ImagePath { get; set; }
        public bool status { get; set; }
        public string BrandName { get; set; }
    }
}