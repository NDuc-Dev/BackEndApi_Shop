using AdminApi.DTOs.Product;
using AdminApi.DTOs.ProductColor;
using AdminApi.DTOs.ProductColorSize;
using AdminApi.Extensions;
using AdminApi.Interfaces;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Controllers
{
    [Route("api/manage/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserServices _userServices;
        private readonly IAuditLogServices _auditlogServices;
        private readonly IProductServices _productServices;
        private readonly ICloudinaryServices _cloudinaryServices;
        private readonly IMapper _mapper;
        public ProductController(ApplicationDbContext context,
        UserServices userServices,
        IAuditLogServices auditlogService,
        IProductServices productServices,
        IMapper mapper,
        ICloudinaryServices cloudinaryServices)
        {
            _context = context;
            _userServices = userServices;
            _auditlogServices = auditlogService;
            _productServices = productServices;
            _mapper = mapper;
            _cloudinaryServices = cloudinaryServices;
        }

        // [HttpGet("get-products")]
        // public async Task<IActionResult> GetProducts([FromQuery] ProductFilters filter, int? pageNumber, int? pageSize)
        // {
        //     int pageSizeValue = pageSize ?? 10;
        //     int pageNumberValue = pageNumber ?? 1;
        //     if (pageNumberValue < 0 || pageSizeValue <= 0)
        //     {
        //         return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
        //         {
        //             Success = false,
        //             Error = new ErrorView()
        //             {
        //                 Code = "INVALID_INPUT",
        //                 Message = "Invalid page number or page size"
        //             }
        //         });
        //     }
        //     var query = _context.Products.AsQueryable();

        //     if (!string.IsNullOrEmpty(filter.Name))
        //     {
        //         query = query.Where(p => p.ProductName.ToLower().Contains(filter.Name.ToLower()));
        //     }
        //     if (filter.Brand.HasValue)
        //     {
        //         query = query.Where(p => p.BrandId == filter.Brand);
        //     }
        //     if (filter.Color != null && filter.Color.Any())
        //     {
        //         query = query.Where(p => p.ProductColor.Any(pc => filter.Color.Contains(pc.ColorId)));
        //     }
        //     if (filter.Size != null && filter.Size.Any())
        //     {
        //         query = query.Where(p => p.ProductColor.Any(pc => pc.ProductColorSizes.Any(pcs => filter.Size.Contains(pcs.SizeId))));
        //     }
        //     try
        //     {
        //         await query
        //             .OrderBy(p => p.ProductId)
        //             .Include(p => p.Brand)
        //             .Include(p => p.NameTags)
        //             .ThenInclude(nt => nt.NameTag)
        //             .Include(p => p.ProductColor)
        //             .Where(p => p.Status == true)
        //             .Skip((pageNumberValue - 1) * pageSizeValue)
        //             .Take(pageSizeValue)
        //             .ToListAsync();
        //         var totalProducts = await query.CountAsync();
        //         var productDtos = _mapper.Map<List<ListProductDto>>(query);
        //         var paginateData = new PaginateDataView<ListProductDto>()
        //         {
        //             ListData = productDtos,
        //             totalCount = totalProducts
        //         };
        //         var response = new ResponseView<PaginateDataView<ListProductDto>>()
        //         {
        //             Success = true,
        //             Message = "Products retrieved successfully !",
        //             Data = paginateData
        //         };
        //         return Ok(response);
        //     }
        //     catch (Exception)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
        //         {
        //             Success = false,
        //             Error = new ErrorView()
        //             {
        //                 Code = "SERVER_ERROR",
        //                 Message = "Error retrieving products !"
        //             }
        //         });
        //     }
        // }

        [HttpGet("get-product/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productServices.GetProductById(id);
            if (product == null) return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
            {
                Success = false,
                Error = new ErrorView()
                {
                    Code = "NOT_FOUND",
                    Message = "Product not found !"
                }
            });
            var productDto = _mapper.Map<ProductDto>(product);
            var result = new ResponseView<ProductDto>()
            {
                Success = true,
                Message = "Get product successfully",
                Data = productDto
            };

            return Ok(result);
        }

        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto model)
        {
            var logs = new List<(User actor, string action, string affectedTable, string objId)>();
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
            string message;
            if (await ValidateProductNameAsync(model.ProductName))
            {
                message = $"Product name {model.ProductName} has been exist, please try with another name";
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
            if (!await ValidateBrandAsync(model.BrandId))
            {
                message = "Brand does not exist, please try again!";
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView
                    {
                        Code = "INVALID_DATA",
                        Message = message
                    }
                });
            };
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var product = await CreateProductAsync(model, user!, logs);
                if (model.NameTagId?.Count > 0)
                {
                    if (!await ValidateNameTagsAsync(model.NameTagId))
                    {
                        return BadRequest(new ResponseView
                        {
                            Success = false,
                            Error = new ErrorView
                            {
                                Code = "INVALID_DATA",
                                Message = "Invalid name tag"
                            }
                        });
                    }
                    await AssignNameTagsToProductAsync(product, model.NameTagId, logs, user!);
                }
                foreach (var variant in model.Variants)
                {
                    if (!await ValidateColorAsync(variant.ColorId))
                    {
                        message = $"Inavalid color id {variant.ColorId}";
                        return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                        {
                            Success = false,
                            Error = new ErrorView
                            {
                                Message = message,
                                Code = "INVALID_DATA"
                            }
                        });
                    };
                    var productColor = await CreateProductColorAsync(product, variant, user!, logs);
                    var sizeIds = variant.ProductColorSize.Select(x => x.SizeId);
                    if (!await ValidateSizesAsync(sizeIds))
                    {
                        message = $"Inavalid size ids";
                        return StatusCode(StatusCodes.Status400BadRequest, new ResponseView<Product>
                        {
                            Success = false,
                            Error = new ErrorView
                            {
                                Message = message,
                                Code = "INVALID_DATA"
                            }
                        });
                    }
                    await AssignSizesToColorAsync(productColor, variant.ProductColorSize, user!, logs);
                }
                await transaction.CommitAsync();
                foreach (var log in logs)
                {
                    await _auditlogServices.LogActionAsync(
                        log.actor,
                        log.action,
                        log.affectedTable,
                        log.objId
                    );
                }
                return StatusCode(StatusCodes.Status200OK, new ResponseView<Product>
                {
                    Success = true,
                    Message = "Product Created Successfully",
                    Data = product

                });
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Create", "Products", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView
                    {
                        Code = "SERVER_ERROR",
                        Message = "An error occurred while adding the product !"
                    }
                });
            }
        }

        [HttpPost("change-product-status/{id}")]
        public async Task<IActionResult> ChangeProductStatus(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            if (!await ValidateProductAsync(id))
            {
                return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "NOT_FOUND",
                        Message = "Product not found !"
                    }
                });
            }
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                await _productServices.ChangeProductStatus(id);
                await transaction.CommitAsync();
                await _auditlogServices.LogActionAsync(user!, "Change status", "Product", id.ToString(), null, Serilog.Events.LogEventLevel.Information);
                var response = new ResponseView()
                {
                    Success = true,
                    Message = "Change product status successfully"
                };
                return Ok(response);
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Change status", "Product", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Have an occured when change product status"
                    }
                });
            }

        }

        #region Private Helper Method
        private async Task<bool> ValidateProductAsync(int productId)
        {
            return await _context.IsExistsAsync<Product>("ProductId", productId);
        }
        private async Task<bool> ValidateProductNameAsync(string productName)
        {
            return await _context.IsExistsAsync<Product>("ProductName", productName);
        }
        private async Task<bool> ValidateBrandAsync(int brandId)
        {
            return await _context.IsExistsAsync<Brand>("BrandId", brandId);
        }
        private async Task<bool> ValidateNameTagsAsync(ICollection<int> nameTagIds)
        {
            if (nameTagIds == null || !nameTagIds.Any()) return true;

            var existingTags = await _context.ProductNameTags
                .Where(nt => nameTagIds.Contains(nt.Id))
                .Select(nt => nt.Id)
                .ToListAsync();

            return existingTags.Count == nameTagIds.Count;
        }
        private async Task<bool> ValidateColorAsync(int colorId)
        {
            return await _context.IsExistsAsync<Color>("ColorId", colorId);
        }
        private async Task<bool> ValidateSizesAsync(IEnumerable<int> sizeIds)
        {
            if (sizeIds == null || !sizeIds.Any()) return true;

            var existingSizes = await _context.Sizes
                .Where(s => sizeIds.Contains(s.SizeId))
                .Select(s => s.SizeId)
                .ToListAsync();

            return existingSizes.Count == sizeIds.Count();
        }
        private async Task<Product> CreateProductAsync(CreateProductDto model, User user, List<(User actor, string action, string affectedTable, string objId)> logs)
        {
            var product = await _productServices.CreateProductAsync(model, user, model.BrandId);
            logs.Add((user, "Create", "Product", product.ProductId.ToString()));
            return product;
        }
        private async Task AssignNameTagsToProductAsync(Product product, ICollection<int> nameTagIds, List<(User actor, string action, string affectedTable, string objId)> logs, User user)
        {
            foreach (var tagId in nameTagIds)
            {
                var productNameTag = await _productServices.CreateProductNameTagAsync(product, tagId);
                logs.Add((user, "Create", "ProductNameTags", productNameTag.Id.ToString()));
            }
        }
        private async Task<ProductColor> CreateProductColorAsync(Product product, CreateProductColorDto variant, User user, List<(User actor, string action, string affectedTable, string objId)> logs)
        {
            var imagesPath = await UploadImagesAsync(variant.images);
            var productColor = await _productServices.CreateProductColorAsync(product, variant.ColorId, variant.Price, imagesPath);
            logs.Add((user, "Create", "ProductColors", productColor.ProductColorId.ToString()));
            return productColor;
        }
        private async Task AssignSizesToColorAsync(ProductColor productColor, List<CreateProductColorSizeDto> sizes, User user, List<(User actor, string action, string affectedTable, string objId)> logs)
        {
            foreach (var size in sizes)
            {
                var productColorSize = await _productServices.CreateProductColorSizeAsync(productColor, size.SizeId, size.Quantity);
                logs.Add((user, "Create", "ProductColorSizes", productColorSize.ProductColorSizeId.ToString()));
            }
        }
        private async Task<string> UploadImagesAsync(IEnumerable<string> images)
        {
            var imagesPath = string.Empty;
            foreach (var image in images)
            {
                var filePath = await _cloudinaryServices.UploadBase64ImageAsync(image, "Products");
                imagesPath += $"{filePath};";
            }
            return imagesPath.TrimEnd(';');
        }
        #endregion
    }
}