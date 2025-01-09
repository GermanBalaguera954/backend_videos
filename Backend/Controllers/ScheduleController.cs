using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // GET: api/schedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules()
        {
            var schedules = await _context.Schedules.Include(s => s.Content).Include(s => s.User).ToListAsync();
            return Ok(schedules);
        }

        // GET: api/schedule/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Schedule>> GetSchedule(int id)
        {
            var schedule = await _context.Schedules.Include(s => s.Content).Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound(new { mensaje = "Programación no encontrada." });
            }

            return Ok(schedule);
        }

        // POST: api/schedule
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Schedule>> PostSchedule(Schedule schedule)
        {
            // Verifica que el contenido existe en la base de datos
            var content = await _context.Contents.FindAsync(schedule.ContentId);
            if (content == null)
            {
                return BadRequest(new { mensaje = "El contenido especificado no existe." });
            }

            // Valida si el contenido es un banner y si la duración está configurada
            if ((content.ContentType == "BT" || content.ContentType == "VBL") && schedule.DurationInSeconds <= 0)
            {
                return BadRequest(new { mensaje = "La duración del banner debe ser mayor a cero segundos." });
            }

            // Verifica si ya existe una programación para el mismo horario
            var overlappingSchedule = await _context.Schedules
                .Where(s => s.ScheduledAt == schedule.ScheduledAt && s.ContentId == schedule.ContentId)
                .FirstOrDefaultAsync();

            if (overlappingSchedule != null)
            {
                return BadRequest(new { mensaje = "Ya existe una programación para este contenido en el mismo horario." });
            }

            // Asignar un usuario por defecto
            if (schedule.UserId == 0)
            {
                schedule.UserId = 1;
            }

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSchedule", new { id = schedule.Id }, schedule);
        }

        // PUT: api/schedule/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSchedule(int id, Schedule schedule)
        {
            if (id != schedule.Id)
            {
                return BadRequest(new { mensaje = "El ID de la programación no coincide." });
            }

            // Verificar si la programación existe
            var existingSchedule = await _context.Schedules.FindAsync(id);
            if (existingSchedule == null)
            {
                return NotFound(new { mensaje = "Programación no encontrada." });
            }

            // Verificar si ya existe una programación para el mismo horario
            var overlappingSchedule = await _context.Schedules
                .Where(s => s.ScheduledAt == schedule.ScheduledAt && s.ContentId == schedule.ContentId && s.Id != id)
                .FirstOrDefaultAsync();

            if (overlappingSchedule != null)
            {
                return BadRequest(new { mensaje = "Ya existe una programación para este contenido en el mismo horario." });
            }

            // Actualizar la programación
            existingSchedule.ContentId = schedule.ContentId;
            existingSchedule.ScheduledAt = schedule.ScheduledAt;
            existingSchedule.DurationInSeconds = schedule.DurationInSeconds;
            existingSchedule.UserId = schedule.UserId;

            _context.Entry(existingSchedule).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/schedule/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                return NotFound(new { mensaje = "Programación no encontrada." });
            }

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // Endpoint para obtener el siguiente contenido programado
        [HttpGet("next/{currentContentId}")]
        public async Task<IActionResult> GetNextScheduledContent(int currentContentId)
        {
            var currentSchedule = await _context.Schedules
                .Where(s => s.ContentId == currentContentId)
                .OrderBy(s => s.ScheduledAt)
                .FirstOrDefaultAsync();

            if (currentSchedule == null)
            {
                return NotFound(new { mensaje = "Contenido no encontrado en la programación." });
            }

            // Buscar el siguiente contenido programado
            var nextSchedule = await _context.Schedules
                .Where(s => s.ScheduledAt > currentSchedule.ScheduledAt)
                .OrderBy(s => s.ScheduledAt)
                .FirstOrDefaultAsync();

            if (nextSchedule == null)
            {
                return NotFound(new { mensaje = "No hay contenido programado para la siguiente reproducción." });
            }

            var nextContent = await _context.Contents.FindAsync(nextSchedule.ContentId);

            if (nextContent == null)
            {
                return NotFound(new { mensaje = "Contenido no encontrado." });
            }

            // Si es un banner, se considera su duración en la programación
            if (nextContent.ContentType == "BT" || nextContent.ContentType == "VBL")
            {
                var nextEndTime = nextSchedule.ScheduledAt.AddSeconds(nextSchedule.DurationInSeconds);
                return Ok(new
                {
                    nextContent.Id,
                    nextContent.Title,
                    nextContent.ContentType,
                    nextContent.VideoUrl,
                    nextContent.BannerImageUrl,
                    nextContent.BannerText,
                    nextSchedule.ScheduledAt,
                    nextEndTime,
                    nextSchedule.DurationInSeconds
                });
            }

            return Ok(new
            {
                nextContent.Id,
                nextContent.Title,
                nextContent.ContentType,
                nextContent.VideoUrl,
                nextContent.BannerImageUrl,
                nextContent.BannerText,
                nextSchedule.ScheduledAt,
                nextSchedule.DurationInSeconds
            });
        }
    }
}
