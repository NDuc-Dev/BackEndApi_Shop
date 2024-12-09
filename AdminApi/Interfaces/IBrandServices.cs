using AdminApi.DTOs;
using AdminApi.DTOs.Brand;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IBrandServices
    {
        Task<Brand> CreateBrandAsync(CreateBrandDto model, User user, string filePath);
        Task<Brand?> GetBrandById(int id);
        Task<List<Brand>> GetBrands();
    }
}