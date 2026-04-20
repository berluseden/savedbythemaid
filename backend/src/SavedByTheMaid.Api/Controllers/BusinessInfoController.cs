using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SavedByTheMaid.Application.DTOs.Business;
using SavedByTheMaid.Domain.Entities;
using SavedByTheMaid.Infrastructure.Data;

namespace SavedByTheMaid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessInfoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BusinessInfoController> _logger;

    public BusinessInfoController(ApplicationDbContext context, ILogger<BusinessInfoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Returns the public business contact info (phone, email, address, hours).
    /// Used by the public Contact page.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BusinessInfoDto>> Get(CancellationToken cancellationToken)
    {
        var info = await _context.BusinessInfos.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (info == null)
            return Ok(new BusinessInfoDto());

        return Ok(ToDto(info));
    }

    /// <summary>
    /// Updates the business contact info. Admin only.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BusinessInfoDto>> Update(
        [FromBody] UpdateBusinessInfoRequest request,
        CancellationToken cancellationToken)
    {
        var info = await _context.BusinessInfos.FirstOrDefaultAsync(cancellationToken);

        if (info == null)
        {
            info = new BusinessInfo { CreatedAt = DateTime.UtcNow };
            _context.BusinessInfos.Add(info);
        }

        info.Phone = request.Phone;
        info.Email = request.Email;
        info.AddressLine1 = request.AddressLine1;
        info.City = request.City;
        info.State = request.State;
        info.ZipCode = request.ZipCode;
        info.WeekdayHours = request.WeekdayHours;
        info.SaturdayHours = request.SaturdayHours;
        info.SundayHours = request.SundayHours;
        info.ResponseTime = request.ResponseTime;
        info.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Business info updated by {User}", User.Identity?.Name);
        return Ok(ToDto(info));
    }

    private static BusinessInfoDto ToDto(BusinessInfo info) => new()
    {
        Phone = info.Phone,
        Email = info.Email,
        AddressLine1 = info.AddressLine1,
        City = info.City,
        State = info.State,
        ZipCode = info.ZipCode,
        WeekdayHours = info.WeekdayHours,
        SaturdayHours = info.SaturdayHours,
        SundayHours = info.SundayHours,
        ResponseTime = info.ResponseTime,
    };
}
