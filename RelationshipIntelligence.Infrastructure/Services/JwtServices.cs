using ContactsManger.Core.DTOs;
using ContactsManger.Core.ServiceContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RelationshipIntelligence.Infrastructure.Services
{
    public class JwtServices : IjwtAuthentication
    {
        private readonly IConfiguration _configuration;
        public JwtServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public AuthentocationRespones Authenticate(Guid userId, string? email, string? personName)
        {
            var expiration = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:expiration_minutes"]));

            Claim[] claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email ?? string.Empty),
                new Claim(ClaimTypes.Name, personName ?? string.Empty),
            };

            var signingKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(signingKey))
                throw new InvalidOperationException(
                    "JWT signing key is not configured. Set the 'Jwt:Key' setting through environment variables or a secret store.");

            SymmetricSecurityKey key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

            SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _configuration["Jwt:issuer"],
                audience: _configuration["Jwt:audience"],
                claims,
                expires: expiration,
                signingCredentials: credentials
            );

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return new AuthentocationRespones
            {
                token = tokenString,
                personeName = personName ?? string.Empty,
                personeEmail = email ?? string.Empty,
                ExpirationTime = expiration,
                refreshToken = GenerateRefreshToken(),
                RefreshTokenExpirationTime = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["RefreshToken:expiration_minutes"]))
            };
        }
    }
}
