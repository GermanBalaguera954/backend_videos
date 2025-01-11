using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly AppDbContext _context;

    public ScheduleController(AppDbContext context)
    {
        _context = context;
    }

    // Endpoint para crear una programación
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleDto scheduleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verifica si el contenido existe
        var content = await _context.Contents.FindAsync(scheduleDto.ContentId);
        if (content == null)
        {
            return BadRequest(new { message = "El contenido no existe." });
        }

        // Calcular EndTime según la duración proporcionada por el usuario
        DateTime endTime = scheduleDto.StartTime.AddSeconds(content.Duration ?? 5);

        // Verificar si la programación se solapa con alguna existente
        var conflictingSchedule = await _context.Schedules
            .Where(s => s.ContentId == scheduleDto.ContentId &&
                        (s.StartTime < endTime && s.EndTime > scheduleDto.StartTime)) // Verificar solapamiento
            .FirstOrDefaultAsync();

        if (conflictingSchedule != null)
        {
            // Si hay un conflicto, se ajusta el StartTime para que espere hasta que el contenido anterior termine
            var adjustedStartTime = conflictingSchedule.EndTime;
            endTime = adjustedStartTime.AddSeconds(content.Duration ?? 5); // Calculamos el nuevo EndTime

            // Devolvemos un mensaje informando que el contenido ha sido ajustado
            return BadRequest(new
            {
                message = $"La programación se solapa con otro contenido. El contenido se reprogramará para comenzar a las {adjustedStartTime:HH:mm:ss}.",
                newStartTime = adjustedStartTime
            });
        }

        // Crear la nueva programación
        var schedule = new Schedule
        {
            ContentId = scheduleDto.ContentId,
            StartTime = scheduleDto.StartTime,
            EndTime = endTime
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetScheduleById), new { id = schedule.Id }, schedule);
    }


    // Endpoint para obtener los contenidos programados
    [HttpGet("scheduled")]
    public async Task<IActionResult> GetScheduledContents()
    {
        var currentTime = DateTime.UtcNow; // Hora actual en UTC

        var scheduledContents = await _context.Schedules
            .Include(s => s.Content)
            .ToListAsync();

        // Convertir las fechas a UTC en el cliente después de traer los datos
        var filteredContents = scheduledContents
            .Where(s => s.StartTime.ToUniversalTime() <= currentTime && s.EndTime.ToUniversalTime() >= currentTime)
            .ToList();

        if (filteredContents == null || !filteredContents.Any())
        {
            return NotFound(new { message = "No hay contenidos programados." });
        }

        var contents = filteredContents.Select(s => s.Content).ToList();
        return Ok(contents);
    }

    // Endpoint para obtener todas las programaciones
    [HttpGet]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await _context.Schedules.Include(s => s.Content).ToListAsync();

        if (schedules == null || !schedules.Any())
        {
            return NotFound(new { message = "No se encontraron programaciones." });
        }

        return Ok(schedules);
    }

    // Endpoint para obtener una programación por ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScheduleById(int id)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Content)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound(new { message = "Programación no encontrada." });
        }

        return Ok(schedule);
    }

    // Endpoint para actualizar una programación
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleDto scheduleDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var schedule = await _context.Schedules.FindAsync(id);
        if (schedule == null)
        {
            return NotFound(new { message = "Programación no encontrada." });
        }

        // Verificar si el contenido existe
        var content = await _context.Contents.FindAsync(scheduleDto.ContentId);
        if (content == null)
        {
            return BadRequest(new { message = "El contenido no existe." });
        }

        // Calcular EndTime según la duración proporcionada por el usuario
        DateTime endTime = scheduleDto.StartTime.AddSeconds(content.Duration ?? 5);

        // Verificar si la programación se solapa con alguna existente
        var conflictingSchedule = await _context.Schedules
            .Where(s => s.ContentId == scheduleDto.ContentId &&
                        s.Id != id && // Excluir la programación actual si se está actualizando
                        (s.StartTime < endTime && s.EndTime > scheduleDto.StartTime)) // Verificar solapamiento de tiempos
            .FirstOrDefaultAsync();

        if (conflictingSchedule != null)
        {
            // Si hay un conflicto, ajustamos el StartTime para que espere hasta que el contenido anterior termine
            var adjustedStartTime = conflictingSchedule.EndTime;
            endTime = adjustedStartTime.AddSeconds(content.Duration ?? 5);

            // Devolvemos un mensaje informando que el contenido ha sido ajustado
            return BadRequest(new
            {
                message = $"La programación se solapa con otro contenido. El contenido se reprogramará para comenzar a las {adjustedStartTime:HH:mm:ss}.",
                newStartTime = adjustedStartTime
            });
        }

        // Actualizar la programación
        schedule.ContentId = scheduleDto.ContentId;
        schedule.StartTime = scheduleDto.StartTime;
        schedule.EndTime = endTime;

        _context.Schedules.Update(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Endpoint para eliminar una programación
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _context.Schedules.FindAsync(id);

        if (schedule == null)
        {
            return NotFound(new { message = "Programación no encontrada." });
        }

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
