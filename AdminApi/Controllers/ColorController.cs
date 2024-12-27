using AdminApi.DTOs.Color;
using AdminApi.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.Models;
using AdminApi.Extensions;
using AdminApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using AdminApi.DTOs.AuditLog;

namespace AdminApi.Controllers
{
    [Authorize("OnlyAdminRole")]
    [Route("api/manage/[controller]")]
    [ApiController]
    public class ColorController : ControllerBase
    {
        private readonly UserServices _userServices;
        private ApplicationDbContext _context;
        private readonly IColorServices _colorServices;
        private readonly IAuditLogServices _auditLogServices;
        private readonly IMapper _mapper;
        public ColorController(UserServices userServices,
        ApplicationDbContext context,
        IColorServices colorServices,
        IAuditLogServices auditLogServices,
        IMapper mapper)
        {
            _context = context;
            _userServices = userServices;
            _colorServices = colorServices;
            _auditLogServices = auditLogServices;
            _mapper = mapper;
        }
        [HttpPost("create-color")]
        public async Task<IActionResult> CreateColor(CreateColorDto model)
        {
            var logs = new List<AuditLogDto>();
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
            if (await _context.IsExistsAsync<Color>("ColorName", model.ColorName!))
            {
                message = $"Color name {model.ColorName} has been exist, please try with another name";
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
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var color = await _colorServices.CreateColorAsync(model, user!);
                    await transaction.CommitAsync();
                    logs.Add(_auditLogServices.CreateLog(user!, "Create", "Color", color.ColorId.ToString()));
                    await _auditLogServices.LogActionAsync(logs);
                    return StatusCode(StatusCodes.Status201Created, new ResponseView<Color>
                    {
                        Success = true,
                        Data = color,
                        Message = "Color created successfully !"
                    });
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    logs.Add(_auditLogServices.CreateLog(user!, "Create", "Color", null, e.ToString(), Serilog.Events.LogEventLevel.Error));
                    await _auditLogServices.LogActionAsync(logs);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "SERVER_ERROR",
                            Message = "An unexpected error occurred while creating the color"
                        }
                    });
                }
            }
        }

        [HttpGet("get-colors")]
        public async Task<IActionResult> GetColors()
        {
            var user = await _userServices.GetCurrentUserAsync();
            var logs = new List<AuditLogDto>();
            try
            {
                var colors = await _colorServices.GetColors();
                if (colors.Count() == 0 || colors == null)
                {
                    return StatusCode(StatusCodes.Status204NoContent, new ResponseView<List<ColorDto>>()
                    {
                        Success = false,
                        Data = null,
                        Message = "Not have color in list"
                    });
                }
                var colorDto = _mapper.Map<List<ColorDto>>(colors);
                return StatusCode(StatusCodes.Status200OK, new ResponseView<List<ColorDto>>()
                {
                    Success = true,
                    Data = colorDto,
                    Message = "Retrive color successfull !"
                });
            }
            catch (Exception e)
            {
                logs.Add(_auditLogServices.CreateLog(user!, "Get Colors", "Color", null, e.ToString(), Serilog.Events.LogEventLevel.Error));
                await _auditLogServices.LogActionAsync(logs);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Error retrieving colors !"
                    }
                });
            }
        }

        [HttpGet("get-color/{id}")]
        public async Task<IActionResult> GetColorById(int id)
        {
            var logs = new List<AuditLogDto>();
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var color = await _colorServices.GetColorById(id);
                if (color == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "NOT_FOUND",
                            Message = "Color not found !"
                        }
                    });
                }
                return StatusCode(StatusCodes.Status200OK, new ResponseView<Color>()
                {
                    Success = true,
                    Message = "Retrived color successfully",
                    Data = color
                });
            }
            catch (Exception e)
            {
                logs.Add(_auditLogServices.CreateLog(user!, "Get Color by id", "Color", null, e.ToString(), Serilog.Events.LogEventLevel.Error));
                await _auditLogServices.LogActionAsync(logs);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Error retrieving color !"
                    }
                });
            }
        }
        [HttpPost("delete-color/{id}")]
        public async Task<IActionResult> DeleteColor(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            var logs = new List<AuditLogDto>();
            if (!await _context.IsExistsAsync<Color>("ColorId", id))
            {
                var respone = new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "NOT_FOUND",
                        Message = "Color not found"
                    }
                };
                return BadRequest(respone);
            }

            var color = await _context.Colors.FindAsync(id);
            if (color!.ProductColor != null)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Can not delete this color because have product color, please delete product color before delete color !"
                    }
                });
            }
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _colorServices.DeleteColor(id);
                    await transaction.CommitAsync();
                    logs.Add(_auditLogServices.CreateLog(user!, "Delete", "Color", id.ToString()));
                    await _auditLogServices.LogActionAsync(logs);
                    return StatusCode(StatusCodes.Status200OK, new ResponseView()
                    {
                        Success = true,
                        Message = "Delete color successfully"
                    });
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    logs.Add(_auditLogServices.CreateLog(user!, "Delete", "Color", null, e.ToString(), Serilog.Events.LogEventLevel.Error));
                    await _auditLogServices.LogActionAsync(logs);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "SERVER_ERROR",
                            Message = "An occurred error while deleting color !"
                        }
                    });
                }
            }
        }
    }
}