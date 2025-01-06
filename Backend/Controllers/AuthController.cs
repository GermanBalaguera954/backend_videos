using Backend.Data;
using Backend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers
{
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

        // Login endpoint
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Buscar usuario por email
            var user = _context.Users.Include(u => u.Role).FirstOrDefault(u => u.Email == login.Email);

            // Verificar si el usuario existe y la contraseña es correcta
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.Password, login.Password) == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { mensaje = "Email o contraseña incorrectos." });
            }

            if (user.Role == null)
            {
                return Unauthorized(new { mensaje = "El usuario no tiene un rol asignado." });
            }

            // Se usa el helper para generar el token
            var token = JwtHelper.GenerateJwtToken(
                _configuration["Jwt:Key"],
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                user.Email,
                user.Name,
                user.Role.Name
            );

            return Ok(new
            {
                Token = token,
                Role = user.Role.Name,
                UserName = user.Name
            });
        }
    }
}
