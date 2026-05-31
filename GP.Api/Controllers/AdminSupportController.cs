using GP.Application.Common;
using GP.Application.DTOs.Support;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GP.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.RequireAdminRole)]
[Route("api/admin/support")]
public class AdminSupportController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminSupportController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("tickets")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminTicketResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllTickets(CancellationToken cancellationToken)
    {
        var tickets = await _dbContext.SupportTickets
            .AsNoTracking()
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AdminTicketResponseDto
            {
                TicketId = t.TicketId,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category.ToString(),
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                UserId = t.UserId,
                UserFullName = ($"{t.User.FirstName} {t.User.FamilyName} {t.User.LastName}").Trim(),
                UserEmail = t.User.Email,
                UserPhone = t.User.Phone
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<AdminTicketResponseDto>>.SuccessResponse(
            tickets,
            "Support tickets retrieved successfully."));
    }

    [HttpPut("tickets/{ticketId:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<AdminTicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTicketStatus(
        [FromRoute] int ticketId,
        [FromBody] UpdateTicketStatusRequestDto dto,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.SupportTickets
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);

        if (ticket == null)
        {
            return NotFound(ApiResponse.ErrorResponse("Support ticket not found."));
        }

        ticket.Status = dto.Status;
        ticket.UpdatedAt = AppTime.GetScheduleNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<AdminTicketResponseDto>.SuccessResponse(
            MapAdminTicketResponse(ticket),
            "Support ticket status updated successfully."));
    }

    private static AdminTicketResponseDto MapAdminTicketResponse(GP.Domain.Entities.SupportTicket ticket)
    {
        return new AdminTicketResponseDto
        {
            TicketId = ticket.TicketId,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category.ToString(),
            Status = ticket.Status.ToString(),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            UserId = ticket.UserId,
            UserFullName = ($"{ticket.User.FirstName} {ticket.User.FamilyName} {ticket.User.LastName}").Trim(),
            UserEmail = ticket.User.Email,
            UserPhone = ticket.User.Phone
        };
    }
}
