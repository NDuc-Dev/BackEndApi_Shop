using AdminApi.DTOs;
using AdminApi.DTOs.NameTag;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface INameTagServices
    {
        Task<NameTag> CreateNameTagAsync(CreateNameTagDto model, User user);
        Task<NameTag?> GetNameTagById(int id);
        Task<List<NameTag>> GetNameTags();
    }
}