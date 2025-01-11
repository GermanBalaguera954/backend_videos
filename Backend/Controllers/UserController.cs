using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserController(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    // Endpoint para crear un usuario
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verifica si el correo ya está registrado
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "El correo electrónico ya está registrado." });
        }

        // Crear un nuevo usuario
        var user = new User
        {
            UserName = userDto.UserName,
            Email = userDto.Email,
            PasswordHash = _passwordHasher.HashPassword(null, userDto.Password),
            Role = userDto.Role ?? "user"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllUsers), new { id = user.Id }, user);
    }


    // Endpoint para obtener todos los usuarios
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();

        if (users == null || !users.Any())
        {
            return NotFound(new { message = "No se encontraron usuarios." });
        }

        return Ok(users);
    }


    // Endpoint para actualizar un usuario
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        user.UserName = userDto.UserName;
        user.Email = userDto.Email;
        user.PasswordHash = _passwordHasher.HashPassword(user, userDto.Password);
        user.Role = userDto.Role ?? user.Role;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    // Endpoint para eliminar un usuario
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
