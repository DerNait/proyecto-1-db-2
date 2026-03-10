using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Services;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] Usuario usuario)
    {
        try
        {
            var creado = await _authService.RegistrarUsuarioAsync(usuario);
            creado.Password = "";
            return Ok(creado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DTO temporal para recibir el login
    public class LoginRequest { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var token = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}