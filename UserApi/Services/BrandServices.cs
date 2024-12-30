using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;
using UserApi.Interfaces;

namespace UserApi.Services
{
    public class BrandServices : IBrandServices
    {
        private readonly ApplicationDbContext _context;
        public BrandServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Brand>> GetBrands()
        {
            return _context.Brands.ToListAsync(); ;
        }

        public Task<Brand?> GetBrandById(int brandId)
        {
            return _context.Brands.FirstOrDefaultAsync(b => b.BrandId == brandId);
        }

    }
}