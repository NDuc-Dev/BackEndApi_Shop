using System.Linq;
using AutoMapper;
using Shared.Data;
using Shared.Models;
using UserApi.DTOs.Brands;

public class MappingProfiles : Profile
{
#nullable disable
    private readonly ApplicationDbContext _context;
    public MappingProfiles(ApplicationDbContext context)
    {
        _context = context;
    }
    public MappingProfiles()
    {
        CreateMap<Brand, BrandDto>()
        .ForMember(dest => dest.BrandId, opt => opt.MapFrom(src => src.BrandId))
        .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.BrandName))
        .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ImagePath))
        .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Descriptions));
    }
}