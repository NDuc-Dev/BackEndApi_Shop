using AdminApi.DTOs;
using AdminApi.DTOs.Product;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IProductServices
    {
        Task<Product> CreateProductAsync(CreateProductDto model, User user, int brandId);
        Task<ProductNameTag> CreateProductNameTagAsync(Product product, int nameTagId);
        Task<ProductColor> CreateProductColorAsync(Product product, int colorId, decimal price, string imagePath);
        Task<ProductColorSize> CreateProductColorSizeAsync(ProductColor productColor, int sizeId, int quantity);
    }
}