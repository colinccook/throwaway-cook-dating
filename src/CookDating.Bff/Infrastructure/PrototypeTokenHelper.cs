using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace CookDating.Bff.Infrastructure;

public static class PrototypeTokenHelper
{
    public static string GenerateJwt(string userId, string email)
    {
        var key = new SymmetricSecurityKey(
            "prototype-key-not-for-production-use-1234567890"u8.ToArray());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("sub", userId),
                new Claim("email", email)
            ],
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
