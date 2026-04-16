using CaseBridge_Users.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace CaseBridge_Users.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        public TokenService(IConfiguration config)
        {
            _config = config;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        }

        public string CreateToken(User user, LawyerProfile? profile = null)
        {
            var claims = new List<Claim>
            {
                // nameid is now an INT string
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                
                // Role-Based Access Control (RBAC)
                new Claim(ClaimTypes.Role, user.UserType)
            };

            // If they are a Lawyer (Junior or Senior), add the SeniorLawyerId claim
            if (profile != null && profile.SeniorLawyerId.HasValue)
            {
                claims.Add(new Claim("SeniorId", profile.SeniorLawyerId.Value.ToString()));
            }
            else if (user.UserType == "Lawyer")
            {
                // If they are a Senior, their 'SeniorId' is effectively their own ID
                claims.Add(new Claim("SeniorId", user.Id.ToString()));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1), // Access token life
                SigningCredentials = creds,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
