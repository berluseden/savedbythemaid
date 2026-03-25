using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class CleaningPlacesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CleaningPlacesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CleaningPlace>>> GetAll()
    {
        return await _context.CleaningPlaces
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Rooms.Where(r => !r.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CleaningPlace>> GetById(int id)
    {
        var place = await _context.CleaningPlaces
            .AsNoTracking()
            .Include(p => p.Rooms.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        return place == null ? NotFound() : place;
    }

    [HttpPost]
    public async Task<ActionResult<CleaningPlace>> Create(CreateCleaningPlaceRequest request)
    {
        var place = new CleaningPlace
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.CleaningPlaces.Add(place);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = place.Id }, place);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCleaningPlaceRequest request)
    {
        var place = await _context.CleaningPlaces.FindAsync(id);
        if (place == null || place.IsDeleted) return NotFound();

        place.Name = request.Name;
        place.Description = request.Description;
        place.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var place = await _context.CleaningPlaces.FindAsync(id);
        if (place == null) return NotFound();

        place.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Rooms
    [HttpGet("{placeId}/rooms")]
    public async Task<ActionResult<IEnumerable<CleaningPlaceRoom>>> GetRooms(int placeId)
    {
        return await _context.CleaningPlaceRooms
            .AsNoTracking()
            .Where(r => r.CleaningPlaceId == placeId && !r.IsDeleted)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync();
    }

    [HttpPost("{placeId}/rooms")]
    public async Task<ActionResult<CleaningPlaceRoom>> AddRoom(int placeId, CreateRoomRequest request)
    {
        var place = await _context.CleaningPlaces.FindAsync(placeId);
        if (place == null || place.IsDeleted) return NotFound("Property type not found");

        var room = new CleaningPlaceRoom
        {
            CleaningPlaceId = placeId,
            Name = request.Name,
            Description = request.Description,
            BaseMinutes = request.BaseMinutes,
            BasePrice = request.BasePrice,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _context.CleaningPlaceRooms.Add(room);
        await _context.SaveChangesAsync();

        return Created($"/api/admin/cleaningplaces/{placeId}/rooms/{room.Id}", room);
    }

    [HttpPut("{placeId}/rooms/{roomId}")]
    public async Task<IActionResult> UpdateRoom(int placeId, int roomId, UpdateRoomRequest request)
    {
        var room = await _context.CleaningPlaceRooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.CleaningPlaceId == placeId);
        
        if (room == null || room.IsDeleted) return NotFound();

        room.Name = request.Name;
        room.Description = request.Description;
        room.BaseMinutes = request.BaseMinutes;
        room.BasePrice = request.BasePrice;
        room.DisplayOrder = request.DisplayOrder;
        room.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{placeId}/rooms/{roomId}")]
    public async Task<IActionResult> DeleteRoom(int placeId, int roomId)
    {
        var room = await _context.CleaningPlaceRooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.CleaningPlaceId == placeId);
        
        if (room == null) return NotFound();

        room.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCleaningPlaceRequest(string Name, string? Description);
public record UpdateCleaningPlaceRequest(string Name, string? Description, bool IsActive);
public record CreateRoomRequest(string Name, string? Description, int BaseMinutes = 15, decimal BasePrice = 10, int DisplayOrder = 0);
public record UpdateRoomRequest(string Name, string? Description, int BaseMinutes, decimal BasePrice, int DisplayOrder, bool IsActive);
