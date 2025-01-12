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

    // Endpoint para obtener los contenidos junto con la programaciones
    [HttpGet("GetScheduledContent")]
    public async Task<IActionResult> GetScheduledContent()
    {
        var localTime = DateTime.Now;  // Obtener la hora local del servidor
        Console.WriteLine("Hora actual en UTC: " + localTime);

        // Consultar los contenidos programados con las fechas de inicio y fin
        var content = await _context.Schedules
            .Include(s => s.Content)  // Cargar el contenido relacionado
            .Where(s => s.StartTime <= localTime && s.EndTime >= localTime)  // Compara con la hora local
            .OrderBy(s => s.StartTime)  // Ordenar por hora de inicio
            .Select(s => new
            {
                Content = s.Content,  // Información del contenido
                StartTime = s.StartTime,  // Información de la programación
                EndTime = s.EndTime     // Información de la programación
            })
            .ToListAsync();

        if (content == null || !content.Any())
        {
            return NotFound(new { message = "No hay contenido programado para este momento." });
        }

        return Ok(content);  // Devuelve los contenidos programados activos
    }

    // Endpoint para crear programaciones
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleDto scheduleDto)
    {
        if (scheduleDto == null)
        {
            return BadRequest("La programación no es válida.");
        }

        // Busca el contenido asociado al ContentId
        var content = await _context.Contents.FindAsync(scheduleDto.ContentId);
        if (content == null)
        {
            return BadRequest("El contenido no existe.");
        }

        // Usa la duración proporcionada en la solicitud o la duración predeterminada del contenido
        var duration = scheduleDto.Duration ?? content.Duration;

        // Verifica si la duración es válida
        if (!duration.HasValue)
        {
            return BadRequest("La duración debe ser proporcionada.");
        }

        // Calcula el EndTime sumando la duración al StartTime
        var startTime = scheduleDto.StartTime;
        var endTime = startTime.AddSeconds(duration.Value);

        // Verifica si hay solapamientos con otros contenidos programados
        var overlappingSchedules = await _context.Schedules
            .Where(s => (startTime >= s.StartTime && startTime < s.EndTime) ||
                        (endTime > s.StartTime && endTime <= s.EndTime) ||
                        (startTime <= s.StartTime && endTime >= s.EndTime))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        string adjustmentMessage = string.Empty;

        // Si se solapa, ajusta el StartTime y EndTime para evitar el conflicto
        if (overlappingSchedules.Any())
        {
            // Obtener la última programación existente
            var lastSchedule = await _context.Schedules.OrderByDescending(s => s.EndTime).FirstOrDefaultAsync();

            if (lastSchedule != null)
            {
                // Ajusta el StartTime del nuevo contenido para que empiece después del último
                startTime = lastSchedule.EndTime.AddSeconds(1);  // 1 segundo después del final de la última programación

                // Volver a calcular el EndTime usando la duración proporcionada
                endTime = startTime.AddSeconds(duration.Value);

                adjustmentMessage = $"El contenido se solapa con otro programado. Se reprogramará para iniciar a las {startTime:yyyy-MM-dd HH:mm:ss}.";
            }
        }

        // Guardar la nueva programación con el EndTime calculado
        var schedule = new Schedule
        {
            ContentId = scheduleDto.ContentId,
            StartTime = startTime,
            EndTime = endTime
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Ok(new { schedule, message = adjustmentMessage });
    }

    // Endpoint para actualizar todas las programaciones
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleDto scheduleDto)
    {
        if (scheduleDto == null)
        {
            return BadRequest("La programación no es válida.");
        }

        // Buscar la programación existente
        var existingSchedule = await _context.Schedules.FindAsync(id);
        if (existingSchedule == null)
        {
            return NotFound("La programación no existe.");
        }

        // Buscar el contenido asociado al ContentId
        var content = await _context.Contents.FindAsync(scheduleDto.ContentId);
        if (content == null)
        {
            return BadRequest("El contenido no existe.");
        }

        // Si no se proporciona duración, se usa la duración del contenido
        var duration = scheduleDto.Duration ?? content.Duration;  // Usa la duración del contenido si no se pasa una duración

        // Verificamos si la duración es válida
        if (!duration.HasValue)
        {
            return BadRequest("La duración debe ser proporcionada.");
        }

        // Calcula el EndTime sumando la duración al StartTime
        var startTime = scheduleDto.StartTime;
        var endTime = startTime.AddSeconds(duration.Value);

        string adjustmentMessage = string.Empty;

        // Verifica si hay solapamientos con otros contenidos programados (excepto la programación que estamos actualizando)
        var overlappingSchedules = await _context.Schedules
            .Where(s => s.Id != id &&  // Asegurarse de que no se compare con sí mismo
                        ((startTime >= s.StartTime && startTime < s.EndTime) ||
                         (endTime > s.StartTime && endTime <= s.EndTime) ||
                         (startTime <= s.StartTime && endTime >= s.EndTime)))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        // Si se solapa, ajustar el StartTime y EndTime para evitar el conflicto
        if (overlappingSchedules.Any())
        {
            // Obtener la última programación existente
            var lastSchedule = await _context.Schedules.OrderByDescending(s => s.EndTime).FirstOrDefaultAsync();

            if (lastSchedule != null)
            {
                // Ajustar el StartTime del contenido actualizado para que empiece después del último
                startTime = lastSchedule.EndTime.AddSeconds(1);  // 1 segundo después del final de la última programación

                // Volver a calcular el EndTime usando la duración proporcionada
                endTime = startTime.AddSeconds(duration.Value);

                adjustmentMessage = $"El contenido se solapa con otro programado. Se reprogramará para iniciar a las {startTime:yyyy-MM-dd HH:mm:ss}.";
            }
        }

        // Actualizar la programación
        existingSchedule.ContentId = scheduleDto.ContentId;
        existingSchedule.StartTime = startTime;
        existingSchedule.EndTime = endTime;

        _context.Schedules.Update(existingSchedule);
        await _context.SaveChangesAsync();

        return Ok(new { existingSchedule, message = adjustmentMessage });
    }

    // Endpoint para obtener programación por ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetScheduleById(int id)
    {
        var schedule = await _context.Schedules.Include(s => s.Content).FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return NotFound(new { message = "Programación no encontrada." });
        }

        return Ok(schedule);
    }

    //Endpoint para obtener todas las programaciones
    [HttpGet]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await _context.Schedules.Include(s => s.Content).ToListAsync();

        if (!schedules.Any())
        {
            return NotFound(new { message = "No se encontraron programaciones." });
        }

        return Ok(schedules);
    }

    // Endpoint para eliminar programación
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
