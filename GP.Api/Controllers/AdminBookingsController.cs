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

    public AdminBookingsController(
        ApplicationDbContext dbContext,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
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
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRefundDecision(
        [FromRoute] int bookingId,
        [FromBody] AdminRefundDecisionDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.ErrorResponse("Invalid request payload."));
        }

        var booking = await _dbContext.Bookings
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

        if (booking == null)
        {
            return NotFound(ApiResponse.ErrorResponse("Booking not found."));
        }

        if (booking.RefundStatus != RefundRequestStatus.Requested)
        {
            return BadRequest(ApiResponse.ErrorResponse("Refund request is not pending."));
        }

        var now = AppTime.GetScheduleNow();

        if (!request.IsApproved)
        {
            booking.RefundStatus = RefundRequestStatus.Rejected;
            booking.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _notificationService.SendNotificationAsync(
                booking.UserId,
                "Refund Rejected",
                "Sorry, your ticket refund request was denied.",
                "تم رفض طلب الاسترداد",
                "نأسف، تم رفض طلب استرداد تذكرتك.",
                "REFUND_REJECTED",
                cancellationToken);

            return Ok(ApiResponse.Ok("Refund request rejected."));
        }

        booking.RefundStatus = RefundRequestStatus.Approved;
        booking.Status = BookingStatus.Cancelled;
        booking.PaymentStatus = PaymentStatus.Refunded;
        booking.UpdatedAt = now;

        booking.User.WalletBalance += booking.TotalPrice;

        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            UserId = booking.UserId,
            Amount = booking.TotalPrice,
            Type = TransactionType.Refund,
            Description = "Ticket refund approved.",
            DescriptionAr = "تمت الموافقة على استرداد قيمة التذكرة",
            BookingId = booking.BookingId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _notificationService.SendNotificationAsync(
            booking.UserId,
            "Refund Approved",
            $"{booking.TotalPrice} EGP has been refunded to your wallet.",
            "تم استرداد المبلغ",
            $"تمت إضافة {booking.TotalPrice} جنيه إلى محفظتك.",
            "REFUND_APPROVED",
            cancellationToken);

        return Ok(ApiResponse.Ok("Refund request approved."));
    }
}
