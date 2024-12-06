using AdminApi.DTOs;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IBrandServices
    {
        Task<Brand> CreateBrandAsync(CreateBrandDTO model, User user, string filePath);
        Task<Brand?> GetBrandById(int id);
        Task<List<Brand>> GetBrands();
    }
}