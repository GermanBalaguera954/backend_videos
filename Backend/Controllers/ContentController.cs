using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/content
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Content>>> GetContents()
        {
            var contents = await _context.Contents.ToListAsync();
            return Ok(contents);
        }

        // GET: api/content/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Content>> GetContent(int id)
        {
            var content = await _context.Contents.FindAsync(id);

            if (content == null)
            {
                return NotFound(new { mensaje = "Contenido no encontrado." });
            }

            return Ok(content);
        }

        // POST: api/content
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Content>> PostContent(Content content)
        {
            // Validar tipo de contenido (VT, VBL, BT)
            if (content.ContentType != "VT" && content.ContentType != "VBL" && content.ContentType != "BT")
            {
                return BadRequest(new { mensaje = "El tipo de contenido debe ser 'VT', 'VBL' o 'BT'." });
            }

            // Validar contenido de tipo 'VBL': debe incluir banner (imagen o texto)
            if (content.ContentType == "VBL" && (string.IsNullOrEmpty(content.BannerImageUrl) && string.IsNullOrEmpty(content.BannerText)))
            {
                return BadRequest(new { mensaje = "El contenido de tipo 'VBL' debe incluir una imagen de banner o un texto de banner." });
            }

            // Agregar nuevo contenido
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetContent", new { id = content.Id }, content);
        }

        // PUT: api/content/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContent(int id, Content content)
        {
            if (id != content.Id)
            {
                return BadRequest(new { mensaje = "El ID del contenido no coincide." });
            }

            var existingContent = await _context.Contents.FindAsync(id);
            if (existingContent == null)
            {
                return NotFound(new { mensaje = "Contenido no encontrado." });
            }

            // Se valida tipo de contenido (VT, VBL, BT)
            if (content.ContentType != "VT" && content.ContentType != "VBL" && content.ContentType != "BT")
            {
                return BadRequest(new { mensaje = "El tipo de contenido debe ser 'VT', 'VBL' o 'BT'." });
            }

            // Se valida el contenido de tipo 'VBL'
            if (content.ContentType == "VBL" && (string.IsNullOrEmpty(content.BannerImageUrl) && string.IsNullOrEmpty(content.BannerText)))
            {
                return BadRequest(new { mensaje = "El contenido de tipo 'VBL' debe incluir una imagen de banner o un texto de banner." });
            }

            // Actualizar el contenido
            existingContent.Title = content.Title;
            existingContent.ContentType = content.ContentType;
            existingContent.VideoUrl = content.VideoUrl;
            existingContent.BannerImageUrl = content.BannerImageUrl;
            existingContent.BannerText = content.BannerText;
            existingContent.DurationInSeconds = content.DurationInSeconds;

            _context.Entry(existingContent).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/content/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContent(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null)
            {
                return NotFound(new { mensaje = "Contenido no encontrado." });
            }

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
