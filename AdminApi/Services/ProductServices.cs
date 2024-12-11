using AdminApi.DTOs.Product;
using AdminApi.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class ProductServices : IProductServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public ProductServices(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<Product> CreateProductAsync(CreateProductDto model, User user, int brandId)
        {
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandId == brandId);
            var productMap = _mapper.Map<Product>(model);
            productMap.Brand = brand;
            productMap.CreatedByUser = user;
            await _context.Products.AddAsync(productMap);
            await _context.SaveChangesAsync();
            return productMap;
        }

        public async Task<ProductNameTag> CreateProductNameTagAsync(Product product, int nameTagId)
        {
            var nameTag = await _context.NameTags.FirstAsync(n => n.NameTagId == nameTagId);
            var productNameTag = new ProductNameTag
            {
                Product = product,
                NameTag = nameTag,
            };
            await _context.ProductNameTags.AddAsync(productNameTag);
            await _context.SaveChangesAsync();
            return productNameTag;
        }

        public async Task<ProductColor> CreateProductColorAsync(Product product, int colorId, decimal price, string imagePath)
        {
            var color = await _context.Colors.FirstAsync(c => c.ColorId == colorId);
            var productColor = new ProductColor
            {
                Product = product,
                Color = color,
                Price = price,
                ImagePath = imagePath
            };
            await _context.ProductColors.AddAsync(productColor);
            await _context.SaveChangesAsync();
            return productColor;
        }

        public async Task<ProductColorSize> CreateProductColorSizeAsync(ProductColor productColor, int sizeId, int quantity)
        {
            var size = await _context.Sizes.FirstAsync(s => s.SizeId == sizeId);
            var productColorSize = new ProductColorSize
            {
                ProductColor = productColor,
                Size = size,
                Quantity = quantity
            };
            await _context.ProductColorSizes.AddAsync(productColorSize);
            await _context.SaveChangesAsync();
            return productColorSize;
        }
        
        public Task<Product?> GetProductById(int id)
        {
            var product = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.CreatedByUser)
            .Include(p => p.NameTags)
            .ThenInclude(nt => nt.NameTag)
            .Include(p => p.ProductColor)
            .ThenInclude(pc => pc.Color)
            .Include(p => p.ProductColor)
            .ThenInclude(pc => pc.ProductColorSizes)
            .ThenInclude(pc => pc.Size)
            .FirstOrDefaultAsync(p => p.ProductId == id);

            return product;
        }
    }
}