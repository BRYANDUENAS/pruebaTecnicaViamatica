using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Entities;

namespace Viamatica.Infrastructure.Security;

public class SecurityOptions
{
    public const string Section = "Security";

    /// <summary>Clave para cifrar campos y claims. Se deriva a 256 bits con SHA-256.</summary>
    public string EncryptionKey { get; set; } = null!;
    public string JwtKey { get; set; } = null!;
    public string JwtIssuer { get; set; } = "viamatica-api";
    public string JwtAudience { get; set; } = "viamatica-app";
    public int JwtMinutes { get; set; } = 120;
}

/// <summary>
/// AES-256-CBC determinista: el IV se deriva del propio texto con HMAC-SHA256.
/// El mismo texto produce siempre el mismo cifrado, lo que permite buscar por
/// correo o identificación sin descifrar toda la tabla.
/// </summary>
public class AesFieldCipher : IFieldCipher
{
    private readonly byte[] _key;

    public AesFieldCipher(IOptions<SecurityOptions> options)
    {
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.Value.EncryptionKey));
    }

    public string Encrypt(string plain)
    {
        var data = Encoding.UTF8.GetBytes(plain);
        var iv = HMACSHA256.HashData(_key, data)[..16];
        using var aes = Aes.Create();
        aes.Key = _key;
        var cipher = aes.EncryptCbc(data, iv);
        return Convert.ToBase64String([.. iv, .. cipher]);
    }

    public string Decrypt(string cipher)
    {
        var raw = Convert.FromBase64String(cipher);
        using var aes = Aes.Create();
        aes.Key = _key;
        return Encoding.UTF8.GetString(aes.DecryptCbc(raw[16..], raw[..16]));
    }
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

/// <summary>Nombres cortos de las claims; sus valores viajan cifrados dentro del JWT.</summary>
public static class AppClaims
{
    public const string UserId = "uid";
    public const string Username = "usr";
    public const string RolId = "rid";
    public const string RolName = "rol";

    public static readonly string[] Encrypted = [UserId, Username, RolId, RolName];
}

public class JwtTokenService : ITokenService
{
    private readonly SecurityOptions _options;
    private readonly IFieldCipher _cipher;

    public JwtTokenService(IOptions<SecurityOptions> options, IFieldCipher cipher)
    {
        _options = options.Value;
        _cipher = cipher;
    }

    public (string Token, DateTime ExpiresAt) Create(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(AppClaims.UserId, _cipher.Encrypt(user.UserId.ToString())),
            new Claim(AppClaims.Username, _cipher.Encrypt(user.Username)),
            new Claim(AppClaims.RolId, _cipher.Encrypt(user.RolId.ToString())),
            new Claim(AppClaims.RolName, _cipher.Encrypt(user.Rol.RolName)),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtKey));
        var expires = DateTime.UtcNow.AddMinutes(_options.JwtMinutes);
        var token = new JwtSecurityToken(
            _options.JwtIssuer,
            _options.JwtAudience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
