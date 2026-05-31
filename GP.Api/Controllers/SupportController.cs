using GP.API.Extensions;
using GP.Application.Common;
using GP.Application.DTOs.Support;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public SupportController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("tickets")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTicket(
        [FromBody] CreateTicketRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));
        }

        var ticket = new SupportTicket
        {
            UserId = userId.Value,
            Title = dto.Title?.Trim() ?? string.Empty,
            Description = dto.Description?.Trim() ?? string.Empty,
            Category = dto.IssueCategory,
            Status = TicketStatus.Open,
            CreatedAt = AppTime.GetScheduleNow()
        };

        _dbContext.SupportTickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(
            MapTicketResponse(ticket),
            "Support ticket created successfully."));
    }

    [HttpGet("tickets")]
    [ProducesResponseType(typeof(ApiResponse<List<TicketResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTickets(CancellationToken cancellationToken)
    {
        var userId = User.GetDomainUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse.ErrorResponse("Invalid user token."));
        }

        var tickets = await _dbContext.SupportTickets
            .AsNoTracking()
            .Where(t => t.UserId == userId.Value)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketResponseDto
            {
                TicketId = t.TicketId,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category.ToString(),
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<TicketResponseDto>>.SuccessResponse(
            tickets,
            "Support tickets retrieved successfully."));
    }

    private static TicketResponseDto MapTicketResponse(SupportTicket ticket)
    {
        return new TicketResponseDto
        {
            TicketId = ticket.TicketId,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category.ToString(),
            Status = ticket.Status.ToString(),
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }
}
