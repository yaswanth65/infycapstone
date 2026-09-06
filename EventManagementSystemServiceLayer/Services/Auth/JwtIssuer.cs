using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EventManagementSystemServiceLayer.Services.Auth
{
   public interface IJwtTokenService
   {
       (string accessToken, DateTime expiresAtUtc) IssueToken(long userId, string userName, string email, string roleName);
   }

   public sealed class JwtTokenService : IJwtTokenService
   {
       private readonly IConfiguration _config;

       public JwtTokenService(IConfiguration config)
       {
           _config = config ?? throw new ArgumentNullException(nameof(config));
       }

       public (string accessToken, DateTime expiresAtUtc) IssueToken(long userId, string userName, string email, string roleName)
       {
           var key = _config["Jwt:Key"] ?? "ThisIsADevelopmentOnlySecretKey_ReplaceInProduction_1234567890";
           var issuer = _config["Jwt:Issuer"] ?? "EventManagementSystem";
           var audience = _config["Jwt:Audience"] ?? "EventManagementSystemUsers";
           if (!int.TryParse(_config["Jwt:ExpirationMinutes"], out var expMinutes) || expMinutes <= 0)
               expMinutes = 60;

           var expiresAt = DateTime.UtcNow.AddMinutes(expMinutes);

           var claims = new List<Claim>
           {
               new(JwtRegisteredClaimNames.Sub, userId.ToString()),
               new(ClaimTypes.NameIdentifier, userId.ToString()),
               new("UserId", userId.ToString()),
               new(ClaimTypes.Name, userName),
               new(JwtRegisteredClaimNames.Email, email),
               new(ClaimTypes.Role, roleName),
               new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
           };

           var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
           var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

           var jwt = new JwtSecurityToken(
               issuer: issuer,
               audience: audience,
               claims: claims,
               notBefore: DateTime.UtcNow,
               expires: expiresAt,
               signingCredentials: creds);

           return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
       }
   }
}
