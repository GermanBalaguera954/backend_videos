using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verifica si el correo o nombre de usuario ya están registrados
        var existingUserEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        if (existingUserEmail != null)
        {
            return BadRequest(new { message = "El correo electrónico ya está registrado." });
        }

        var existingUserName = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userDto.UserName);
        if (existingUserName != null)
        {
            return BadRequest(new { message = "El nombre de usuario ya está registrado." });
        }

        // Verifica si la contraseña no es nula
        if (string.IsNullOrEmpty(userDto.Password))
        {
            return BadRequest(new { message = "La contraseña es obligatoria." });
        }

        // Crear un nuevo usuario
        var user = new User
        {
            UserName = userDto.UserName,
            Email = userDto.Email,
            PasswordHash = _passwordHasher.HashPassword(new User(), userDto.Password),
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

        // Convertimos a DTO para no incluir el passwordHash ni el password
        var userDtos = users.Select(user => new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role
        }).ToList();

        return Ok(userDtos);
    }

    // Endpoint para actualizar un usuario
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
            return NotFound(new { message = $"Usuario con id {id} no encontrado." });
        }

        // Solo actualizar los campos necesarios
        user.UserName = userDto.UserName;
        user.Email = userDto.Email;

        // Si la contraseña es proporcionada, la hasheamos
        if (!string.IsNullOrEmpty(userDto.Password))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, userDto.Password);
        }

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
            return NotFound(new { message = $"Usuario con id {id} no encontrado." });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}