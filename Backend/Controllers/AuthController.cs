using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthController(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    // Endpoint de login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto login)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Busca el usuario por correo electrónico
        var user = _context.Users.FirstOrDefault(u => u.Email == login.Email);

        // Verifica si el usuario no existe o la contraseña es incorrecta
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, login.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos." });
        }

        // Generar el JWT con el email y el username del usuario
        var token = JwtHelper.GenerateJwtToken(
            _configuration["Jwt:Key"],
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            user.Email,
            user.UserName
        );

        // Devuelve el token junto con el email y el username
        return Ok(new
        {
            Token = token,
            Correo = user.Email,
            NombreUsario = user.UserName
        });
    }
}
