using AdminApi.DTOs.Size;
using AdminApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class SizeServices :ISizeServices
    {
        private readonly ApplicationDbContext _context;
        public SizeServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Size> CreateSizeAsync(CreateSizeDto model, User user)
        {
            var size = new Size()
            {
                SizeValue = model.SizeValue,
                CreateBy = user
            };
            await _context.Sizes.AddAsync(size);
            await _context.SaveChangesAsync();
            return size;
        }

        public Task<Size?> GetSizeById(int id)
        {
            return _context.Sizes.FirstOrDefaultAsync(s => s.SizeId == id);
        }

        public Task<List<Size>> GetSizes()
        {
            return _context.Sizes.ToListAsync();
        }
    }
}