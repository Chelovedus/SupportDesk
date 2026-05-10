using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Application.Tickets;
using SupportDesk.Domain;
using SupportDesk.Domain.Users;

namespace SupportDesk.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    
    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TicketListItemResponse>>> GetAllTickets(
        [FromQuery] TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();
            
            var tickets = await _ticketService.GetAllTickets(
                userId: userId,
                role: role,
                request: request,
                cancellationToken: cancellationToken);
            
            return Ok(tickets);
        }
        catch (DomainException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> GetTicket(int id, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.GetTicketById(ticketId: id, userId: actorId, role: role, cancellationToken: cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ticket = await _ticketService.CreateTicket(request: request, userId: userId, cancellationToken: cancellationToken);

            return CreatedAtAction(
                actionName: nameof(GetTicket),
                routeValues: new { id = ticket.Id },
                value: ticket);
        }
        catch (DomainException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<ActionResult<TicketResponse>> AssignTicket(int id, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.AssignTicket(ticketId: id, actorId: actorId, role: role, request: request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<ActionResult<TicketResponse>> StartTicket(int id, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.StartProgressTicket(ticketId: id, actorId: actorId, role: role, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/resolve")]
    [Authorize(Roles = "SupportAgent,Admin")]
    public async Task<ActionResult<TicketResponse>> ResolveTicket(int id, ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.ResolveTicket(ticketId: id, actorId: actorId, request: request, role: role, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "User,Admin")]
    public async Task<ActionResult<TicketResponse>> CloseTicket(int id, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.CloseTicket(ticketId: id, actorId: actorId, role: role, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "User,Admin")]
    public async Task<ActionResult<TicketResponse>> CancelTicket(int id, CancelTicketRequest request, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.CancelTicket(ticketId: id, actorId: actorId, role: role, request: request, cancellationToken: cancellationToken));
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<TicketCommentResponse>>> GetCommentsForTicket(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.GetComments(ticketId: id, userId: userId, role: role, cancellationToken: cancellationToken));
    }
    
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateCommentForTicket(int id, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.AddComment(ticketId: id, actorId: actorId, role: role, request: request, cancellationToken: cancellationToken));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyCollection<TicketHistoryItemResponse>>> GetHistoryForTicket(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        return await ExecuteTicketAction(() => _ticketService.GetHistory(ticketId: id, userId: userId, role: role, cancellationToken: cancellationToken));
    }

    private Guid GetCurrentUserId()
    {
        var value = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new InvalidOperationException("User id claim is missing.");
        
        return userId;
    }

    private UserRole GetCurrentUserRole()
    {
        var value = HttpContext.User.FindFirstValue(ClaimTypes.Role);
        
        if (!Enum.TryParse<UserRole>(value, out var role))
            throw new InvalidOperationException("User role claim is missing.");
        
        return role;
    }
    
    private async Task<ActionResult<TResponse>> ExecuteTicketAction<TResponse>(Func<Task<TResponse?>> action)
    {
        try
        {
            var response = await action();

            if (response is null)
                return NotFound();
            
            return Ok(response);
        }
        catch (DomainException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}