using System.Linq;
using AdminApi.DTOs.Brand;
using AdminApi.DTOs.Color;
using AdminApi.DTOs.NameTag;
using AdminApi.DTOs.Product;
using AdminApi.DTOs.ProductColor;
using AdminApi.DTOs.ProductColorSize;
using AdminApi.DTOs.Size;
using AutoMapper;
using Shared.Data;
using Shared.Models;

public class MappingProfile : Profile
{
#nullable disable
    private readonly ApplicationDbContext _context;
    public MappingProfile(ApplicationDbContext context)
    {
        _context = context;
    }
    public MappingProfile()
    {

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.ProductDescription, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ProductColor.Select(pc => pc.ImagePath.Split(';', System.StringSplitOptions.RemoveEmptyEntries).First()).First()));

        CreateMap<NameTag, NameTagDto>()
            .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.NameTagId));

        CreateMap<ProductColor, ProductColorDto>()
            .ForMember(dest => dest.ColorId, opt => opt.MapFrom(src => src.ColorId))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.ProductColorId, opt => opt.MapFrom(src => src.ProductColorId))
            .ForMember(dest => dest.ProductColorSize, opt => opt.MapFrom(src => src.ProductColorSizes))
            .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ImagePath.Split(';', System.StringSplitOptions.RemoveEmptyEntries).ToList()));


        // CreateMap<ProductColorDto, ProductColor>()
        //     .ForMember(dest => dest.ColorId, opt => opt.MapFrom(src => src.ColorId))
        //     .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.UnitPrice))
        //     .ForMember(dest => dest.ProductColorId, opt => opt.MapFrom(src => src.ProductColorId))
        //     .ForMember(dest => dest.Product, opt => opt.Ignore())
        //     .ForMember(dest => dest.Color, opt => opt.Ignore());

        CreateMap<ProductColorSize, ProductColorSizeDto>()
            .ForMember(dest => dest.ProductColorSizeId, opt => opt.MapFrom(src => src.ProductColorSizeId))
            .ForMember(dest => dest.ProductColorId, opt => opt.MapFrom(src => src.ProductColorId))
            .ForMember(dest => dest.SizeId, opt => opt.MapFrom(src => src.SizeId))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));


        CreateMap<Product, ProductListDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.ProductDescription, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.Brand.BrandName))
            .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ProductColor.Select(pc => pc.ImagePath.Split(';', System.StringSplitOptions.RemoveEmptyEntries).First()).First()));

        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ProductDescription))
            .ForMember(dest => dest.Brand, opt => opt.Ignore());


        CreateMap<Brand, BrandDto>()
            .ForMember(dest => dest.BrandId, opt => opt.MapFrom(src => src.BrandId))
            .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.BrandName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Descriptions))
            .ForMember(dest => dest.ImagePath, opt => opt.MapFrom(src => src.ImagePath));

        CreateMap<Color, ColorDto>()
            .ForMember(dest => dest.ColorId, opt => opt.MapFrom(src => src.ColorId))
            .ForMember(dest => dest.ColorName, opt => opt.MapFrom(src => src.ColorName));

        CreateMap<Size, SizeDto>()
            .ForMember(dest => dest.SizeId, opt => opt.MapFrom(src => src.SizeId))
            .ForMember(dest => dest.SizeValue, opt => opt.MapFrom(src => src.SizeValue));
    }
}