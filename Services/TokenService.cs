using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CatatanKeuanganDotnet.Models;
using CatatanKeuanganDotnet.Options;
using CatatanKeuanganDotnet.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CatatanKeuanganDotnet.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public TokenService(IOptions<JwtOptions> options)
        {
            _jwtOptions = options.Value;
            if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
            {
                throw new InvalidOperationException("JWT key belum dikonfigurasi.");
            }
        }

        public string GenerateToken(User user)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Key);
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("fullName", user.FullName),
                new Claim("isActive", user.IsActive.ToString())
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: identity.Claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresMinutes),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }
    }
}