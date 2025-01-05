using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Authorize]
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

        // GET: api/schedules/5
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

        // POST: api/schedules
        [HttpPost]
        public async Task<ActionResult<Schedule>> PostSchedule(Schedule schedule)
        {
            // Verificar que el contenido existe en la base de datos
            var content = await _context.Contents.FindAsync(schedule.ContentId);
            if (content == null)
            {
                return BadRequest(new { mensaje = "El contenido especificado no existe." });
            }

            // Verificar si ya existe una programación para el mismo horario
            var overlappingSchedule = await _context.Schedules
                .Where(s => s.ScheduledAt == schedule.ScheduledAt && s.ContentId == schedule.ContentId)
                .FirstOrDefaultAsync();

            if (overlappingSchedule != null)
            {
                return BadRequest(new { mensaje = "Ya existe una programación para este contenido en el mismo horario." });
            }

            // Asignar un usuario por defecto (si es necesario)
            if (schedule.UserId == 0)
            {
                schedule.UserId = 1;  // Supongamos que 1 es el ID del administrador por defecto
            }

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSchedule", new { id = schedule.Id }, schedule);
        }

        // PUT: api/schedules/5
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

        // DELETE: api/schedules/5
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
    }
}
