using AdminApi.DTOs.Color;
using AdminApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class ColorServices : IColorServices
    {
        private readonly ApplicationDbContext _context;
        public ColorServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Color> CreateColorAsync(CreateColorDto model, User user)
        {
            var color = new Color()
            {
                ColorName = model.ColorName,
                CreateBy = user
            };
            await _context.Colors.AddAsync(color);
            await _context.SaveChangesAsync();
            return color;
        }

        public Task<Color?> GetColorById(int id)
        {
            return _context.Colors.FirstOrDefaultAsync(c => c.ColorId == id);
        }

        public Task<List<Color>> GetColors()
        {
            return _context.Colors.ToListAsync();
        }

        public async Task DeleteColor(int id)
        {
            var color = await _context.Colors.FindAsync(id);
            _context.Colors.Remove(color!);
            await _context.SaveChangesAsync();
            await Task.CompletedTask;
        }
    }
}