using AdminApi.DTOs.NameTag;
using AdminApi.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.Models;
using AdminApi.Extensions;
using AutoMapper;
using AdminApi.Interfaces;

namespace AdminApi.Controllers
{
    [Route("api/manage/[controller]")]
    [ApiController]
    public class NameTagController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogServices _auditlogServices;
        private readonly UserServices _userServices;
        private readonly INameTagServices _nametagServices;
        private readonly IMapper _mapper;
        public NameTagController(ApplicationDbContext context,
        IAuditLogServices auditLogService,
        UserServices userServices,
        INameTagServices nametagServices,
        IMapper mapper)
        {
            _context = context;
            _auditlogServices = auditLogService;
            _userServices = userServices;
            _nametagServices = nametagServices;
            _mapper = mapper;
        }

        [HttpPost("create-name-tag")]
        public async Task<IActionResult> CreateNameTag(CreateNameTagDto model)
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
            var message = "";
            var user = await _userServices.GetCurrentUserAsync();
            if (user == null) return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
            {
                Success = false,
                Error = new ErrorView()
                {
                    Code = "NOT_FOUND",
                    Message = "User not found !"
                }
            });
            if (await _context.IsExistsAsync<NameTag>("Tag", model.TagName!))
            {
                message = $"Tag name {model.TagName} has been exist, please try with another name";
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
                    var nameTag = await _nametagServices.CreateNameTagAsync(model, user);
                    var result = new ResponseView<NameTag>()
                    {
                        Success = true,
                        Data = nameTag,
                        Message = "Create name tag successfully !"
                    };
                    await transaction.CommitAsync();
                    await _auditlogServices.LogActionAsync(user, "Create", "NameTags", nameTag.NameTagId.ToString());
                    return Ok(result);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                    {
                        Success = false,
                        Error = new ErrorView()
                        {
                            Code = "SERVER_ERROR",
                            Message = "Have an occured error while create name tag !"
                        }
                    });
                }
            }
        }

        [HttpGet("get-name-tag/{id}")]
        public async Task<IActionResult> GetNameTagById(int id)
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var nameTag = await _nametagServices.GetNameTagById(id);
                if (nameTag == null) return StatusCode(StatusCodes.Status404NotFound, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "NOT_FOUND",
                        Message = "Name tag not found !"
                    }
                });
                var result = new ResponseView<NameTag>()
                {
                    Success = true,
                    Message = "Retrived name tag successfully",
                    Data = nameTag
                };
                return Ok(result);
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Get", "NameTags", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Have an occured error while create name tag !"
                    }
                });
            }
        }
        
        [HttpGet("get-name-tag")]
        public async Task<IActionResult> GetNameTags()
        {
            var user = await _userServices.GetCurrentUserAsync();
            try
            {
                var nametags = await _nametagServices.GetNameTags();
                if (nametags.Count() == 0 || nametags == null)
                {
                    return StatusCode(StatusCodes.Status204NoContent, new ResponseView<List<NameTagDto>>()
                    {
                        Success = false,
                        Data = null,
                        Message = "Not have name tag in list"
                    });
                }
                var nameTagDtos = _mapper.Map<List<NameTagDto>>(nametags);
                var result = new ResponseView<List<NameTagDto>>()
                {
                    Success = true,
                    Data = nameTagDtos,
                    Message = "Retrive name tag successfull !"
                };
                return Ok(result);
            }
            catch (Exception e)
            {
                await _auditlogServices.LogActionAsync(user!, "Get", "NameTags", null, e.ToString(), Serilog.Events.LogEventLevel.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseView()
                {
                    Success = false,
                    Error = new ErrorView()
                    {
                        Code = "SERVER_ERROR",
                        Message = "Have an occured error while retrive name tag !"
                    }
                });
            }

        }
    }
}