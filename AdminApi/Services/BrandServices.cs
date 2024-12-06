using AdminApi.DTOs;
using AdminApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class BrandServices : IBrandServices
    {
        private readonly ApplicationDbContext _context;
        public BrandServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Brand> CreateBrandAsync(CreateBrandDTO model, User user, string filePath)
        {
            var brand = new Brand
            {
                BrandName = model.BrandName,
                Descriptions = model.Descriptions,
                CreatedByUser = user,
                ImagePath = Path.GetFileName(filePath)
            };
            await _context.Brands.AddAsync(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public Task<Brand?> GetBrandById(int id)
        {
            return _context.Brands.FirstOrDefaultAsync(b => b.BrandId == id);
        }

        public Task<List<Brand>> GetBrands()
        {
            return _context.Brands.ToListAsync(); ;
        }
    }
}