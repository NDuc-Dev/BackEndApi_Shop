using AdminApi.DTOs.Color;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IColorServices
    {
        Task<Color> CreateColorAsync(CreateColorDto model, User user);
        Task<Color?> GetColorById(int id);
        Task<List<Color>> GetColors();
        Task DeleteColor(int id);
    }
}