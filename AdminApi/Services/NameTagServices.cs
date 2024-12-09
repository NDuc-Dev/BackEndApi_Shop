using AdminApi.DTOs.NameTag;
using AdminApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class NameTagServices : INameTagServices
    {
        private readonly ApplicationDbContext _context;
        public NameTagServices(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<NameTag> CreateNameTagAsync(CreateNameTagDto model, User user)
        {
            var nameTag = new NameTag()
            {
                Tag = model.TagName,
                CreateBy = user
            };
            await _context.NameTags.AddAsync(nameTag);
            await _context.SaveChangesAsync();
            return nameTag;
        }

        public Task<NameTag?> GetNameTagById(int id)
        {
            return _context.NameTags.FirstOrDefaultAsync(n => n.NameTagId == id);
        }
        public Task<List<NameTag>> GetNameTags()
        {
            return _context.NameTags.ToListAsync();
        }
    }
}