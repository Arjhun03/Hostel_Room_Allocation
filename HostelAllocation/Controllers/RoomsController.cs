
using HostelAllocation.Data;
using HostelAllocation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HostelAllocation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Room>>> GetRooms(
        [FromQuery] bool availableOnly = false)
    {
        IQueryable<Room> query = _context.Rooms;

        if (availableOnly)
        {
            query = query.Where(room =>
                room.OccupiedBeds < room.Capacity);
        }

        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Room>> GetRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> CreateRoom(Room room)
    {
        if (!room.IsValidOccupiedBeds())
        {
            ModelState.AddModelError(
                nameof(room.OccupiedBeds),
                "Occupied beds cannot exceed room capacity.");

            return ValidationProblem(ModelState);
        }

        _context.Rooms.Add(room);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException sqlException
                  && (sqlException.Number == 2601
                      || sqlException.Number == 2627))
        {
            return Conflict(new
            {
                message = $"Room number '{room.RoomNumber}' already exists."
            });
        }

        return CreatedAtAction(
            nameof(GetRoom),
            new { id = room.Id },
            room);
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRoom(int id, Room updatedRoom)
    {
        if (id != updatedRoom.Id)
            return BadRequest("URL ID and body ID must match.");

        if (!updatedRoom.IsValidOccupiedBeds())
        {
            ModelState.AddModelError(
                nameof(updatedRoom.OccupiedBeds),
                "Occupied beds cannot exceed room capacity.");

            return ValidationProblem(ModelState);
        }

        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
            return NotFound();

        room.RoomNumber = updatedRoom.RoomNumber;
        room.RoomType = updatedRoom.RoomType;
        room.Capacity = updatedRoom.Capacity;
        room.OccupiedBeds = updatedRoom.OccupiedBeds;
        room.MonthlyFee = updatedRoom.MonthlyFee;

        await _context.SaveChangesAsync();

        return Ok(room);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
            return NotFound();

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
