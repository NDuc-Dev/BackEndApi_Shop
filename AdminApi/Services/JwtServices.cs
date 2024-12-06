using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdminApi.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Shared.Data;
using Shared.Models;

namespace AdminApi.Services
{
    public class JwtServices : IJwtServices
    {
        private readonly IConfiguration _config;
        private SymmetricSecurityKey _jwtKey;
        private readonly ApplicationDbContext _context;
        public JwtServices(IConfiguration config, ApplicationDbContext context)
        {
            _context = context;
            _config = config;
            _jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!));

        }

        public string CreateJwt(User user, IList<string> userRole)
        {
            List<Claim> userClaims = GetClaim(user, userRole);
            var credentials = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(userClaims),
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["JWT:ExpiresInDays"]!)),
                SigningCredentials = credentials,
                Issuer = _config["JWT:Issuer"]
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(jwt);
        }

        #region Private Helper Method
        private List<Claim> GetClaim(User user, IList<string> userRole)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };
            foreach (var role in userRole)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }
        #endregion
    }
}