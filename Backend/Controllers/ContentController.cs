using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContentController(AppDbContext context)
    {
        _context = context;
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateContent([FromBody] ContentDto contentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var content = new Content
        {
            Title = contentDto.Title,
            ContentType = contentDto.ContentType,
            VideoUrl = contentDto.VideoUrl,
            BannerImageUrl = contentDto.BannerImageUrl,
            BannerText = contentDto.BannerText,
            Duration = contentDto.Duration,
            UserId = 1
        };

        _context.Contents.Add(content);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetContentById), new { id = content.Id }, content);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllContents()
    {
        var contents = await _context.Contents.ToListAsync();

        if (contents == null || !contents.Any())
        {
            return NotFound(new { message = "No se encontraron contenidos." });
        }

        return Ok(contents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContentById(int id)
    {
        var content = await _context.Contents
                                    .Include(c => c.User)
                                    .FirstOrDefaultAsync(c => c.Id == id);

        if (content == null)
        {
            return NotFound(new { message = "Contenido no encontrado." });
        }

        return Ok(content);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateContent(int id, [FromBody] ContentDto contentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var content = await _context.Contents.FindAsync(id);

        if (content == null)
        {
            return NotFound(new { message = "Contenido no encontrado." });
        }

        // Obtener la duración anterior
        var previousDuration = content.Duration;

        // Actualizar los campos
        content.Title = contentDto.Title;
        content.ContentType = contentDto.ContentType;
        content.VideoUrl = contentDto.VideoUrl;
        content.BannerImageUrl = contentDto.BannerImageUrl;
        content.BannerText = contentDto.BannerText;
        content.Duration = contentDto.Duration;

        _context.Contents.Update(content);
        await _context.SaveChangesAsync();

        // Si la duración del contenido ha cambiado, actualizar las programaciones relacionadas
        if (previousDuration != content.Duration)
        {
            var schedulesToUpdate = await _context.Schedules
                .Where(s => s.ContentId == content.Id)
                .ToListAsync();

            foreach (var schedule in schedulesToUpdate)
            {
                // Calcular el nuevo EndTime según la nueva duración
                var startTime = schedule.StartTime;
                var newEndTime = startTime.AddSeconds(content.Duration.Value);

                // Actualizar la programación con el nuevo EndTime
                schedule.EndTime = newEndTime;
                _context.Schedules.Update(schedule);
            }

            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        var content = await _context.Contents.FindAsync(id);

        if (content == null)
        {
            return NotFound(new { message = "Contenido no encontrado." });
        }

        // Verificar si el contenido pertenece al usuario autenticado
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (content.UserId != int.Parse(userId))
        {
            return Unauthorized(new { message = "No tienes permisos para eliminar este contenido." });
        }

        _context.Contents.Remove(content);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
