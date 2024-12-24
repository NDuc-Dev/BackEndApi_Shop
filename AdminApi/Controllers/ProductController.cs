using AdminApi.DTOs.NameTag;
using AdminApi.DTOs.Product;
using AdminApi.DTOs.ProductColor;
using AdminApi.DTOs.ProductColorSize;
using AdminApi.Extensions;
using AdminApi.Interfaces;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Controllers
{
    [Authorize("OnlyAdminRole")]
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

        [HttpGet("get-products")]
        public async Task<IActionResult> GetProducts([FromQuery] string name, int? brandId, int? pageNumber, int? pageSize)
        {
            int pageSizeValue = pageSize ?? 10;
            int pageNumberValue = pageNumber ?? 1;
            if (pageNumberValue < 0 || pageSizeValue <= 0)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "INVALID_INPUT",
                        Message = "Invalid page number or page size"
                    }
                });
            }
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.ProductName.ToLower().Contains(name.ToLower()));
            }
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId);
            }
            try
            {
                await query
                    .OrderBy(p => p.ProductId)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductColor)
                    .Skip((pageNumberValue - 1) * pageSizeValue)
                    .Take(pageSizeValue)
                    .ToListAsync();
                var totalProducts = await query.CountAsync();
                var productDtos = _mapper.Map<List<ProductListDto>>(query);
                var paginateData = new PaginateDataView<ProductListDto>()
                {
                    ListData = productDtos,
                    totalCount = totalProducts
                };
                var response = new ResponseView<PaginateDataView<ProductListDto>>()
                {
                    Success = true,
                    Message = "Products retrieved successfully !",
                    Data = paginateData
                };
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Error retrieving products !"
                    }
                });
            }
        }

        [HttpGet("get-product-variant/{id}")]
        public async Task<IActionResult> GetProductVariantById(int id)
        {
            var productVariant = await _productServices.GetProductVariantById(id);
            if (productVariant == null) return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
            {
                Success = false,
                Error = new ErrorView()
                {
                    Code = "NOT_FOUND",
                    Message = "Variant not found !"
                }
            });
            var variantDto = _mapper.Map<ProductColorDto>(productVariant);
            return Ok(variantDto);
        }

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

        [HttpPost("update-product")]
        public async Task<IActionResult> UpdateBaseInfoProduct(ProductDto model)
        {
            var logs = new List<(User actor, string action, string affectedTable, string objId)>();
            var user = await _userServices.GetCurrentUserAsync();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ErrorViewForModelState
                {
                    Success = false,
                    Error = new ErrorModelStateView
                    {
                        Code = "INVALID_INPUT",
                        Errors = errors
                    }
                });
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView
                {
                    Success = false,
                    Message = "Product not found",
                    Error = new ErrorView
                    {
                        Code = "NOT_FOUND",
                        Message = "Product does not exist"
                    }
                });
            }
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                product.ProductName = model.ProductName;
                product.Description = model.ProductDescription;

                // Commit transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Ghi logs
                foreach (var log in logs)
                {
                    await _auditlogServices.LogActionAsync(log.actor, log.action, log.affectedTable, log.objId);
                }

                return Ok(new ResponseView<Product>
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView
                {
                    Success = false,
                    Message = "An error occurred while updating the product",
                    Error = new ErrorView
                    {
                        Code = "SERVER_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpPost("update-variant-images")]
        public async Task<IActionResult> UpdateImageForVariant(int variantId, List<string> base64Images)
        {
            var user = await _userServices.GetCurrentUserAsync();
            var variant = await _context.ProductColors.FirstOrDefaultAsync(pc => pc.ProductColorId == variantId);
            if (variant == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "NOT_FOUND",
                        Message = "Variant not found !"
                    }
                });
            }
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                string newImagePath = await UploadImagesAsync(base64Images);
                variant.ImagePath = newImagePath;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await _auditlogServices.LogActionAsync(user!, "Update", "ProductColors", variant.ProductColorId.ToString(), null);
                return Ok(new ResponseView
                {
                    Success = true,
                    Message = "Product images updated successfully !"
                });
            }
            catch (Exception ex)
            {
                await _auditlogServices.LogActionAsync(user!, "Update", "ProductColors", variant.ProductColorId.ToString(), ex.Message.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView
                {
                    Success = false,
                    Message = "An error occurred while updating images of product",
                    Error = new ErrorView
                    {
                        Code = "SERVER_ERROR",
                        Message = ex.Message
                    }
                });
            }

        }

        [HttpPost("update-product-name-tag")]
        public async Task<IActionResult> UpdateProductNameTags(int? productId, List<NameTagDto> model)
        {
            var user = await _userServices.GetCurrentUserAsync();
            if (productId == null || !await ValidateProductAsync(productId))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "INVALID_DATA",
                        Message = "Invalid product id"
                    }
                });
            }
            if (model == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "INVALID_DATA",
                        Message = "List name tag is required"
                    }
                });
            }
            try
            {
                var transaction = await _context.Database.BeginTransactionAsync();
                var existingNameTags = await _context.ProductNameTags.Where(pt => pt.ProductId == productId).Select(pt => pt.NameTagId).ToListAsync();
                var nameTagsToAdd = model.Select(n => n.TagId).Except(existingNameTags).ToList();
                var nameTagsToRemove = existingNameTags.Except(model.Select(n => n.TagId)).ToList();

                // Thêm NameTags mới
                if (!await ValidateProductNameTagsAsync(nameTagsToAdd))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new ResponseView
                    {
                        Success = false,
                        Message = "Invalid NameTag",
                        Error = new ErrorView
                        {
                            Code = "INVALID_DATA",
                            Message = "One or more NameTags are invalid."
                        }
                    });
                }
                foreach (var tagId in nameTagsToAdd)
                {
                    var newNameTag = new ProductNameTag { ProductId = (int)productId, NameTagId = tagId };
                    _context.ProductNameTags.Add(newNameTag);
                    await _auditlogServices.LogActionAsync(user!, "Create", "ProductNameTags", newNameTag.Id.ToString());
                }

                // Xoá NameTags không còn
                foreach (var tagId in nameTagsToRemove)
                {
                    var tagToRemove = await _context.ProductNameTags.Where(pt => pt.ProductId == productId && pt.NameTagId == tagId).FirstOrDefaultAsync();
                    if (tagToRemove != null)
                    {
                        _context.ProductNameTags.Remove(tagToRemove);
                        await _auditlogServices.LogActionAsync(user!, "Delete", "ProductNameTags", tagToRemove.Id.ToString());
                    }
                }
                return StatusCode(StatusCodes.Status200OK, new ResponseView()
                {
                    Success = true,
                    Message = "Update product name tags successfully !"
                });
            }
            catch (Exception ex)
            {
                await _auditlogServices.LogActionAsync(user!, "Delete", "ProductNameTags", null, ex.Message, Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView
                {
                    Success = false,
                    Message = "An error occurred while updating product name tags",
                    Error = new ErrorView
                    {
                        Code = "SERVER_ERROR",
                        Message = ex.Message
                    }
                });
            }
        }
        #region Private Helper Method
        private Task<bool> ValidateProductAsync(int? productId)
        {
            return _context.IsExistsAsync<Product>("ProductId", productId!);
        }
        private Task<bool> ValidateProductNameAsync(string productName)
        {
            return _context.IsExistsAsync<Product>("ProductName", productName);
        }
        private Task<bool> ValidateBrandAsync(int brandId)
        {
            return _context.IsExistsAsync<Brand>("BrandId", brandId);
        }
        private async Task<bool> ValidateProductNameTagsAsync(ICollection<int> nameTagIds)
        {
            if (nameTagIds == null || !nameTagIds.Any()) return true;

            var existingTags = await _context.ProductNameTags
                .Where(nt => nameTagIds.Contains(nt.Id))
                .Select(nt => nt.Id)
                .ToListAsync();

            return existingTags.Count == nameTagIds.Count;
        }
        private async Task<bool> ValidateNameTagsAsync(ICollection<int> nameTagIds)
        {
            if (nameTagIds == null || !nameTagIds.Any()) return false;
            var existingTags = await _context.NameTags
                .Where(nt => nameTagIds.Contains(nt.NameTagId))
                .Select(nt => nt.NameTagId)
                .ToListAsync();

            return existingTags.Count == nameTagIds.Count;
        }
        private Task<bool> ValidateColorAsync(int colorId)
        {
            return _context.IsExistsAsync<Color>("ColorId", colorId);
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

        // private async Task UpdateProductColors(Product existingProduct, List<ProductColorDto> productColors)
        // {
        //     if (productColors == null) return;

        //     foreach (var colorDto in productColors)
        //     {
        //         ProductColor? existingColor = null;

        //         // Nếu biến thể đã tồn tại, cập nhật
        //         if (colorDto.ProductColorId.HasValue)
        //         {
        //             existingColor = existingProduct.ProductColor
        //                 .FirstOrDefault(pc => pc.ProductColorId == colorDto.ProductColorId.Value);

        //             if (existingColor != null)
        //             {
        //                 existingColor.ColorId = colorDto.ColorId;
        //                 existingColor.Price = colorDto.UnitPrice;

        //                 // Xử lý ảnh
        //                 existingColor.ImagePath = await SaveImages(colorDto.Images);

        //                 // Xử lý ProductColorSize
        //                 UpdateProductColorSizes(existingColor, colorDto.ProductColorSizes);
        //             }
        //         }
        //         else // Nếu biến thể chưa tồn tại, thêm mới
        //         {
        //             var newColor = new ProductColor
        //             {
        //                 ProductId = existingProduct.ProductId,
        //                 ColorId = colorDto.ColorId,
        //                 Price = colorDto.Price,
        //                 ImagePath = await SaveImages(colorDto.Images),
        //                 ProductColorSizes = new List<ProductColorSize>()
        //             };

        //             // Thêm kích thước mới (nếu có)
        //             if (colorDto.ProductColorSizes != null)
        //             {
        //                 foreach (var sizeDto in colorDto.ProductColorSizes)
        //                 {
        //                     newColor.ProductColorSizes.Add(new ProductColorSize
        //                     {
        //                         SizeId = sizeDto.SizeId,
        //                         Quantity = sizeDto.Quantity
        //                     });
        //                 }
        //             }

        //             existingProduct.ProductColor.Add(newColor);
        //         }
        //     }
        // }
        #endregion
    }
}