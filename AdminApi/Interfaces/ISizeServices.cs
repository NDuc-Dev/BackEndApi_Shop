using AdminApi.DTOs;
using AdminApi.DTOs.Size;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface ISizeServices
    {
        Task<Size> CreateSizeAsync(CreateSizeDto model, User user);
        Task<Size?> GetSizeById(int id);
        Task<List<Size>> GetSizes();
    }
}