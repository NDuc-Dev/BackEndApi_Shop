using AdminApi.DTOs.Product;
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
        private readonly IImageServices _imageServices;
        private readonly IMapper _mapper;
        public ProductController(ApplicationDbContext context,
        UserServices userServices,
        IAuditLogServices auditlogService,
        IProductServices productServices,
        IImageServices imageServices,
        IMapper mapper)
        {
            _context = context;
            _userServices = userServices;
            _auditlogServices = auditlogService;
            _productServices = productServices;
            _imageServices = imageServices;
            _mapper = mapper;
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
            if (await _context.IsExistsAsync<Product>("ProductName", model.ProductName))
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
            if (!await _context.IsExistsAsync<Brand>("BrandId", model.BrandId))
            {
                message = "Brand does not exist, please try again!";
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Message = message,
                    Error = new ErrorView
                    {
                        Code = "INVALID_DATA",
                        Message = message
                    }
                });
            };
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var product = await _productServices.CreateProductAsync(model, user!, model.BrandId);
                    logs.Add((user!, "Create", "Products", product.ProductId.ToString()));
                    if (model.NameTagId != null && model.NameTagId.Count > 0)
                    {
                        foreach (var tagId in model.NameTagId)
                        {
                            if (!await _context.IsExistsAsync<NameTag>("NameTagId", tagId))
                            {
                                await transaction.RollbackAsync();
                                message = "Inavalid name tag";
                                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                                {
                                    Success = false,
                                    Message = message,
                                    Error = new ErrorView
                                    {
                                        Message = message,
                                        Code = "INVALID_DATA"
                                    }
                                });
                            }
                            var productNameTag = await _productServices.CreateProductNameTagAsync(product, tagId);
                            logs.Add((user!, "Create", "ProductNameTags", productNameTag.Id.ToString()));

                        }
                    }
                    foreach (var variant in model.Variants)
                    {
                        var imagesPath = string.Empty;
                        foreach (var image in variant.images)
                        {
                            var filePath = _imageServices.CreatePathForBase64Img("products", image);
                            imagesPath += $"{filePath};";
                        }
                        imagesPath = imagesPath.TrimEnd(';');
                        if (!await _context.IsExistsAsync<Color>("ColorId", variant.ColorId))
                        {
                            await transaction.RollbackAsync();
                            message = $"Inavalid color id {variant.ColorId}";
                            return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                            {
                                Success = false,
                                Message = message,
                                Error = new ErrorView
                                {
                                    Message = message,
                                    Code = "INVALID_DATA"
                                }
                            });
                        };
                        var productColor = await _productServices.CreateProductColorAsync(product, variant.ColorId, variant.Price, imagesPath);
                        await _auditlogServices.LogActionAsync(user!, "Create", "ProductColors", productColor.ProductColorId.ToString());

                        foreach (var size in variant.ProductColorSize)
                        {
                            if (!await _context.IsExistsAsync<Size>("SizeId", size.SizeId))
                            {
                                await transaction.RollbackAsync();
                                message = $"Inavalid size id {size.SizeId}";
                                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView<Product>
                                {
                                    Success = false,
                                    Message = message,
                                    Error = new ErrorView
                                    {
                                        Message = message,
                                        Code = "INVALID_DATA"
                                    }
                                });
                            }
                            var productColorSize = await _productServices.CreateProductColorSizeAsync(productColor, size.SizeId, size.Quantity);
                            logs.Add((user!, "Create", "ProductColorSizes", productColorSize.ProductColorSizeId.ToString()));
                        }
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
                    await transaction.RollbackAsync();
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
        }

        [HttpPost("change-product-status/{id}")]
        public async Task<IActionResult> ChangeProductStatus(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            if (!await _context.IsExistsAsync<Product>("ProductId", id))
            {
                await _auditlogServices.LogActionAsync(user!, "Change Status", "Product", null, $"Not found product have product id = {id}", Serilog.Events.LogEventLevel.Warning);
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
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
                    product!.Status = product.Status ? false : true;
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditlogServices.LogActionAsync(user!, "Change status", "Product", product.ProductId.ToString(), null, Serilog.Events.LogEventLevel.Information);
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
        }
    }
}