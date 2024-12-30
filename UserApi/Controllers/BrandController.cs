using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using UserApi.DTOs.Brands;
using UserApi.Interfaces;

namespace UserApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly IBrandServices _brandServices;
        private readonly IMapper _mapper;
        public BrandController(IBrandServices brandServices, IMapper mapper)
        {
            _brandServices = brandServices;
            _mapper = mapper;
        }

        [HttpGet("get-brands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _brandServices.GetBrands();
            var brandDto = _mapper.Map<List<BrandDto>>(brands);
            return Ok(brandDto);
        }

        [HttpGet("get-brand/{id}")]
        public async Task<IActionResult> GetBrandById(int id)
        {
            var brand = await _brandServices.GetBrandById(id);
            var brandDto = _mapper.Map<BrandDto>(brand);
            return Ok(brandDto);
        }
    }
}