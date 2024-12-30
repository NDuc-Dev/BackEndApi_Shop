using Shared.Models;

namespace UserApi.Interfaces
{
    public interface IBrandServices
    {
        Task<List<Brand>> GetBrands();
        Task<Brand?> GetBrandById(int brandId);
    }
}