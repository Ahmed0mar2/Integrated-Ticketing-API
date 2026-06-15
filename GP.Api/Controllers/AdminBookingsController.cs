using GP.Application.Common;
using GP.Application.DTOs.Admin;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GP.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.RequireAdminRole)]
[Route("api/admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly IBookingService _bookingService;

    public AdminBookingsController(
        ApplicationDbContext dbContext,
        INotificationService notificationService,
        IBookingService bookingService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _bookingService = bookingService;
    }

    [HttpGet("bookings/refund-requests")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminRefundResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRefundRequests(CancellationToken cancellationToken)
    {
        var refundRequests = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.RefundStatus != null)
            .Include(b => b.User)
            .Include(b => b.OriginStation)
            .Include(b => b.DestinationStation)
            .Include(b => b.Occurrence)
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new AdminRefundResponseDto
            {
                BookingId = b.BookingId,
                UserId = b.UserId,
                UserFullName = ($"{b.User.FirstName} {b.User.FamilyName} {b.User.LastName}").Trim(),
                UserEmail = b.User.Email,
                UserPhone = b.User.Phone,
                TotalPrice = b.TotalPrice,
                RefundStatus = b.RefundStatus!.ToString()!,
                BookingStatus = b.Status.ToString(),
                OriginStationName = b.OriginStation.EnglishName,
                DestinationStationName = b.DestinationStation.EnglishName,
                DepartureTime = b.Occurrence.DepartureDateTime,
                UpdatedAt = b.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<AdminRefundResponseDto>>.SuccessResponse(
            refundRequests,
            "Refund requests retrieved successfully."));
    }

    [HttpPut("bookings/{bookingId:int}/refund")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ProcessRefundDecision(
    [FromRoute] int bookingId,
    [FromBody] AdminRefundDecisionDto request,
    CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(ApiResponse.ErrorResponse("Invalid request payload."));

        try
        {
            var resultMessage = await _bookingService.ProcessRefundDecisionAsync(bookingId, request, cancellationToken);
            return Ok(ApiResponse.Ok(resultMessage));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
    }
}
