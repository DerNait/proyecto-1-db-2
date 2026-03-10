using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Services;

public class AuthService
{
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IConfiguration _config;

    public AuthService(IMongoDatabase database, IConfiguration config)
    {
        _usuarios = database.GetCollection<Usuario>("usuarios");
        _config = config;
    }

    public async Task<Usuario> RegistrarUsuarioAsync(Usuario nuevoUsuario)
    {
        // Verificar si el correo ya existe
        var existe = await _usuarios.Find(u => u.Email == nuevoUsuario.Email).AnyAsync();
        if (existe) throw new Exception("El correo ya está registrado.");

        // Encriptar la contraseña con BCrypt
        nuevoUsuario.Password = BCrypt.Net.BCrypt.HashPassword(nuevoUsuario.Password);
        nuevoUsuario.FechaRegistro = DateTime.UtcNow;

        await _usuarios.InsertOneAsync(nuevoUsuario);
        return nuevoUsuario;
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        // Buscar al usuario
        var usuario = await _usuarios.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (usuario == null) throw new Exception("Credenciales incorrectas.");

        // Verificar la contraseña encriptada
        if (!BCrypt.Net.BCrypt.Verify(password, usuario.Password))
            throw new Exception("Credenciales incorrectas.");

        // Generar los "Claims" (Datos que viajan dentro del token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id!),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre)
        };

        // Firmar el Token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}