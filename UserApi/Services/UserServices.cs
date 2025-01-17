using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserApi.DTOs.Email;
using UserApi.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class UserServices
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailServices _emailServices;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserServices(IConfiguration config, ApplicationDbContext context, UserManager<User> userManager, IEmailServices emailService, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _context = context;
            _userManager = userManager;
            _emailServices = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<User?> GetCurrentUserAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;
            return _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
        public async Task<User?> GetUserInfoFromJwtAsync(string authorizationHeader)
        {
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!)),
                ValidIssuer = _config["JWT:Issuer"],
                ValidateIssuer = true,
                ValidateAudience = false
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                await Task.Delay(10);
                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> SendForgotPassword(User user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"{_config["JWT:ClientUrl"]}/{_config["Email:ResetPasswordPath"]}?token={token}&email={user.Email}";
            var body = $"<p>Hello: {user.FullName}</p> " +
                $"<p>Please click <a href =\"{url}\">here</a> to reset your password</p>" +
                "<p>Thank you</p>" +
                $"<br>{_config["Email:ApplicationName"]}";

            var emaiSend = new EmailSendDto(user.Email!, "RESET PASSWORD", body);

            return await _emailServices.SendEmail(emaiSend);
        }

        public async Task<string> GenerateDefaultPassword()
        {
            Random random = new Random();

            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string specialChars = "!@#$%^&*()";
            const string digits = "0123456789";

            char upperChar = upperChars[random.Next(upperChars.Length)];
            char lowerChar = lowerChars[random.Next(lowerChars.Length)];
            char specialChar = specialChars[random.Next(specialChars.Length)];

            string numberString = new string(Enumerable.Repeat(digits, 3).Select(s => s[random.Next(s.Length)]).ToArray());

            string result = upperChar.ToString() + lowerChar + specialChar + numberString;
            await Task.Delay(10);
            return new string(result.ToCharArray().OrderBy(c => random.Next()).ToArray());
        }
    }
}