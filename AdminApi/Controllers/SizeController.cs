using System.Transactions;
using AdminApi.DTOs.Size;
using AdminApi.Interfaces;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Controllers
{
    [Route("api/manage/[controller]")]
    [ApiController]
    public class SizeController : ControllerBase
    {
        private ApplicationDbContext _context;
        private readonly ISizeServices _sizeServices;
        private readonly IAuditLogServices _auditlogServices;
        private readonly UserServices _userServices;
        private readonly IMapper _mapper;
        public SizeController(ApplicationDbContext context,
        ISizeServices sizeServices,
        IAuditLogServices auditlogServices,
        UserServices userServices,
        IMapper mapper)
        {
            _context = context;
            _sizeServices = sizeServices;
            _auditlogServices = auditlogServices;
            _userServices = userServices;
            _mapper = mapper;
        }

        [HttpGet("get-sizes")]
        public async Task<IActionResult> GetSizes()
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var sizes = await _sizeServices.GetSizes();
                if (sizes.Count() == 0 || sizes == null)
                {
                    return StatusCode(StatusCodes.Status204NoContent, new ResponseView<List<SizeDto>>()
                    {
                        Success = false,
                        Data = null,
                        Message = "Not have size in list"
                    });
                }
                var sizeDtos = _mapper.Map<List<SizeDto>>(sizes);
                var result = new ResponseView<List<SizeDto>>()
                {
                    Success = true,
                    Data = sizeDtos,
                    Message = "Retrive size successfull !"
                };
                return Ok(result);
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Get", "Sizes", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Have an occured error while retrive sizes !"
                    }
                });
            }

        }

        [HttpGet("get-size/{id}")]
        public async Task<IActionResult> GetSizeById(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var size = await _sizeServices.GetSizeById(id);
                if (size == null) return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "NOT_FOUND",
                        Message = "Size not found !"
                    }
                });
                var result = new ResponseView<Size>()
                {
                    Success = true,
                    Message = "Get size successfully",
                    Data = size
                };
                return Ok(result);
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Get", "Sizes", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Have an occured error while get size !"
                    }
                });
            }
        }

        [HttpPost("create-size")]
        public async Task<IActionResult> CreateSize(CreateSizeDto model)
        {
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
            var user = await _userServices.GetCurrentUserAsync();
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var size = await _sizeServices.CreateSizeAsync(model, user!);
                    await transaction.CommitAsync();
                    await _auditlogServices.LogActionAsync(user!, "Create", "Sizes", size.SizeId.ToString());
                    return StatusCode(StatusCodes.Status201Created, new ResponseView<Size>()
                    {
                        Success = true,
                        Message = "Brand Created Successfully",
                        Data = size
                    });
                }
                catch (Exception e)
                {
                    await _auditlogServices.LogActionAsync(user!, "Create", "Sizes", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "SERVER_ERROR",
                            Message = "An occured error while creating size !"
                        }
                    });
                }
            }
        }
    }
}