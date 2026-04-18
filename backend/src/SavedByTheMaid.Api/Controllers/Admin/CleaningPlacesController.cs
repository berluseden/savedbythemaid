using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Api.Auth;
using SavedByTheMaid.Api.Extensions;
using SavedByTheMaid.Infrastructure.Data;
using SavedByTheMaid.Domain.Entities;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class CleaningPlacesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<CreateCleaningPlaceRequest> _createPlaceValidator;
    private readonly IValidator<UpdateCleaningPlaceRequest> _updatePlaceValidator;
    private readonly IValidator<CreateRoomRequest> _createRoomValidator;
    private readonly IValidator<UpdateRoomRequest> _updateRoomValidator;

    public CleaningPlacesController(
        ApplicationDbContext context,
        IValidator<CreateCleaningPlaceRequest> createPlaceValidator,
        IValidator<UpdateCleaningPlaceRequest> updatePlaceValidator,
        IValidator<CreateRoomRequest> createRoomValidator,
        IValidator<UpdateRoomRequest> updateRoomValidator)
    {
        _context = context;
        _createPlaceValidator = createPlaceValidator;
        _updatePlaceValidator = updatePlaceValidator;
        _createRoomValidator = createRoomValidator;
        _updateRoomValidator = updateRoomValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CleaningPlace>>> GetAll(CancellationToken cancellationToken = default)
    {
        return await _context.CleaningPlaces
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Rooms.Where(r => !r.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CleaningPlace>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var place = await _context.CleaningPlaces
            .AsNoTracking()
            .Include(p => p.Rooms.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        return place == null ? NotFound() : place;
    }

    [HttpPost]
    public async Task<ActionResult<CleaningPlace>> Create(CreateCleaningPlaceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createPlaceValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var place = new CleaningPlace
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _context.CleaningPlaces.Add(place);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = place.Id }, place);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCleaningPlaceRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updatePlaceValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var place = await _context.CleaningPlaces.FindAsync(new object[] { id }, cancellationToken);
        if (place == null || place.IsDeleted) return NotFound();

        place.Name = request.Name;
        place.Description = request.Description;
        place.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var place = await _context.CleaningPlaces.FindAsync(new object[] { id }, cancellationToken);
        if (place == null) return NotFound();

        place.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{placeId}/rooms")]
    public async Task<ActionResult<IEnumerable<CleaningPlaceRoom>>> GetRooms(int placeId, CancellationToken cancellationToken = default)
    {
        return await _context.CleaningPlaceRooms
            .AsNoTracking()
            .Where(r => r.CleaningPlaceId == placeId && !r.IsDeleted)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    [HttpPost("{placeId}/rooms")]
    public async Task<ActionResult<CleaningPlaceRoom>> AddRoom(int placeId, CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _createRoomValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var place = await _context.CleaningPlaces.FindAsync(new object[] { placeId }, cancellationToken);
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
        await _context.SaveChangesAsync(cancellationToken);

        return Created($"/api/admin/cleaningplaces/{placeId}/rooms/{room.Id}", room);
    }

    [HttpPut("{placeId}/rooms/{roomId}")]
    public async Task<IActionResult> UpdateRoom(int placeId, int roomId, UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateRoomValidator.ValidateAndReturnErrors(request);
        if (validationError != null) return validationError;

        var room = await _context.CleaningPlaceRooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.CleaningPlaceId == placeId, cancellationToken);

        if (room == null || room.IsDeleted) return NotFound();

        room.Name = request.Name;
        room.Description = request.Description;
        room.BaseMinutes = request.BaseMinutes;
        room.BasePrice = request.BasePrice;
        room.DisplayOrder = request.DisplayOrder;
        room.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{placeId}/rooms/{roomId}")]
    public async Task<IActionResult> DeleteRoom(int placeId, int roomId, CancellationToken cancellationToken = default)
    {
        var room = await _context.CleaningPlaceRooms
            .FirstOrDefaultAsync(r => r.Id == roomId && r.CleaningPlaceId == placeId, cancellationToken);

        if (room == null) return NotFound();

        room.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateCleaningPlaceRequest(string Name, string? Description);
public record UpdateCleaningPlaceRequest(string Name, string? Description, bool IsActive);
public record CreateRoomRequest(string Name, string? Description, int BaseMinutes = 15, decimal BasePrice = 10, int DisplayOrder = 0);
public record UpdateRoomRequest(string Name, string? Description, int BaseMinutes, decimal BasePrice, int DisplayOrder, bool IsActive);
