using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContentController(AppDbContext context)
    {
        _context = context;
    }

    // Endpoint para crear contenido
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateContent([FromBody] ContentDto contentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Crear un nuevo contenido
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

    // Endpoint para obtener todos los contenidos
    [HttpGet]
    public async Task<IActionResult> GetAllContents()
    {
        var contents = await _context.Contents
                                    .Include(c => c.User)
                                    .ToListAsync();

        if (contents == null || !contents.Any())
        {
            return NotFound(new { message = "No se encontraron contenidos." });
        }

        return Ok(contents);
    }

    // Endpoint para obtener un contenido por su ID
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

    // Endpoint para actualizar un contenido
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

        // Actualizar los campos
        content.Title = contentDto.Title;
        content.ContentType = contentDto.ContentType;
        content.VideoUrl = contentDto.VideoUrl;
        content.BannerImageUrl = contentDto.BannerImageUrl;
        content.BannerText = contentDto.BannerText;
        content.Duration = contentDto.Duration;

        _context.Contents.Update(content);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Endpoint para eliminar un contenido
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        var content = await _context.Contents.FindAsync(id);

        if (content == null)
        {
            return NotFound(new { message = "Contenido no encontrado." });
        }

        _context.Contents.Remove(content);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
