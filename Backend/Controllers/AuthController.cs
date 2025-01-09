using Backend.Data;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


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

        // Lógica para asignar un rol de admin por defecto si el correo y la contraseña coinciden con los valores predeterminados
        if (login.Email == "admin@example.com" && login.Password == "Segura123")
        {
            // Aquí creamos un usuario con rol de "admin" por defecto
            var adminRole = "Admin";
            var token = JwtHelper.GenerateJwtToken(
                _configuration["Jwt:Key"],
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                login.Email,
                "SuperAdmin",
                adminRole
            );

            return Ok(new
            {
                Token = token,
                Role = adminRole,
                UserName = "SuperAdmin"
            });
        }

        // Si no es el admin por defecto, buscamos al usuario en la base de datos
        var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == login.Email);

        // Verificar si el usuario existe y si la contraseña es correcta
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.Password, login.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos." });
        }

        if (user.Role == null)
        {
            return Unauthorized(new { mensaje = "El usuario no tiene un rol asignado." });
        }

        // Generar el JWT con el rol que está asignado al usuario
        var anotherToken = JwtHelper.GenerateJwtToken(
            _configuration["Jwt:Key"],
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            user.Email,
            user.Name,
            user.Role.Name
        );

        return Ok(new
        {
            Token = anotherToken,
            Role = user.Role.Name,
            UserName = user.Name
        });
    }
}
