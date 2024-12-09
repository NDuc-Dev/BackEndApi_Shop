using AdminApi.DTOs.Brand;
using AdminApi.Interfaces;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;
using WebIdentityApi.Extensions;

namespace AdminApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly IBrandServices _brandServices;
        private readonly IMapper _mapper;
        private readonly IAuditLogServices _auditLog;
        private ApplicationDbContext _context;
        private readonly IImageServices _imageServices;
        public BrandController(UserServices userServices,
        IBrandServices brandServices,
        IMapper mapper,
        IAuditLogServices auditLog,
        ApplicationDbContext context,
        IImageServices imageServices)
        {
            _userServices = userServices;
            _brandServices = brandServices;
            _mapper = mapper;
            _auditLog = auditLog;
            _context = context;
            _imageServices = imageServices;
        }

        [HttpGet("get-brands")]
        public async Task<IActionResult> GetBrands()
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var brands = await _brandServices.GetBrands();
                if (brands == null)
                {
                    return StatusCode(StatusCodes.Status204NoContent, new ResponseView()
                    {
                        Success = false,
                        Message = "Not have brand in list"
                    });
                }
                var brandDto = _mapper.Map<List<BrandDto>>(brands);
                var totalCount = brandDto.Count();
                var paginateData = new PaginateDataView<BrandDto>()
                {
                    ListData = brandDto,
                    totalCount = totalCount
                };
                return StatusCode(StatusCodes.Status200OK, new ResponseView<PaginateDataView<BrandDto>>
                {
                    Success = true,
                    Data = paginateData,
                    Message = "Retrive brand successfully !"
                });
            }
            catch (Exception e)
            {
                await _auditLog.LogActionAsync(user!, "Get brands", "Brand", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Error retrieving brand !"
                    }
                });
            }
        }

        [HttpGet("get-brand/{id}")]
        public async Task<IActionResult> GetBrandById(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var brand = await _brandServices.GetBrandById(id);
                if (brand == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "NOT_FOUND",
                            Message = "Brand not found !"
                        }
                    });
                }
                var brandDto = _mapper.Map<BrandDto>(brand);
                return StatusCode(StatusCodes.Status200OK, new ResponseView<BrandDto>()
                {
                    Success = true,
                    Data = brandDto,
                    Message = "Get brand successfully !"
                });
            }
            catch (Exception e)
            {
                await _auditLog.LogActionAsync(user!, "Get brand by id", "Brand", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "An occured error when get brand !"
                    }
                });
            }
        }

        [HttpPost("create-brand")]
        public async Task<IActionResult> CreateBrand([FromForm] CreateBrandDto model)
        {
            var user = await _userServices.GetCurrentUserAsync();
            if (!ModelState.IsValid)
            {
                var err = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                var respone = new ErrorViewForModelState()
                {
                    Success = false,
                    Error = new ErrorModelStateView()
                    {
                        Code = "INVALID_INPUT",
                        Errors = err
                    }
                };
                return BadRequest(respone);
            }
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (!_imageServices.ProcessImageExtension(model.Image))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                        {
                            Success = false,
                            Error = new ErrorView
                            {
                                Code = "INVALID_DATA",
                                Message = "Invalid image format. Only JPG, JPEG, and PNG files are allowed."
                            }
                        });
                    }
                    string filePath = await _imageServices.CreatePathForImg("brands", model.Image);
                    if (await _context.IsExistsAsync<Brand>("BrandName", model.BrandName))
                    {
                        var message = $"Brand name {model.BrandName} has been exist, please try with another name";
                        return StatusCode(StatusCodes.Status400BadRequest, new ResponseView
                        {
                            Success = false,
                            Message = message,
                            Error = new ErrorView
                            {
                                Code = "DUPPLICATE_NAME",
                                Message = message
                            }
                        });
                    }
                    var brand = await _brandServices.CreateBrandAsync(model, user!, filePath);
                    var brandDto = _mapper.Map<BrandDto>(brand);
                    await transaction.CommitAsync();
                    await _auditLog.LogActionAsync(user!, "Create", "Brand", brandDto.BrandId.ToString(), null);
                    return StatusCode(StatusCodes.Status201Created, new ResponseView<Brand>()
                    {
                        Success = true,
                        Message = "Brand Created Successfully",
                        Data = brand
                    });
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    await _auditLog.LogActionAsync(user!, "Create", "Brand", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "SERVER_ERROR",
                            Message = "An occured error while creating brand !"
                        }
                    });
                }
            }
        }

        // [HttpPut("update-brand/{id}")]
        // public async Task<IActionResult> UpdateBrand(int id, [FromForm] UpdateBrandDto model)
        // {
        //     if (model.DataChanged == false) return Ok("No data field changed!");
        //     var brand = await _context.Brands.FirstOrDefaultAsync(b => b.BrandId == id);
        //     if (brand == null) return BadRequest("Brand does not exist, please try again");
        //     string fileImagePath = null;
        //     if (model.ImageChanged == true)
        //     {
        //         fileImagePath = await _imageServices.CreatePathForImg("brands", model.Image);
        //     }
        //     using (var transaction = await _context.Database.BeginTransactionAsync())
        //     {

        //         var exitsName = await _context.Brands.FirstOrDefaultAsync(b => b.BrandName == model.BrandName);
        //         if (exitsName != null) throw new Exception($"Brand {model.BrandName} has been exist, please try with another name");
        //         try
        //         {
        //             brand.BrandName = model.BrandName;
        //             brand.Descriptions = model.Descriptions;
        //             if (fileImagePath != null)
        //             {
        //                 brand.ImagePath = fileImagePath;
        //             }
        //             _context.Update(brand);
        //             await transaction.CommitAsync();
        //             return Ok("Brand has been update !");
        //         }
        //         catch (Exception ex)
        //         {
        //             await transaction.RollbackAsync();
        //             return BadRequest(ex.ToString());
        //         }
        //     }
        // }
    }
}