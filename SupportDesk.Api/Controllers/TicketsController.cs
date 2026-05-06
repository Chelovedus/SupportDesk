using Microsoft.AspNetCore.Mvc;
using SupportDesk.Contracts.Requests;
using SupportDesk.Contracts.Responses;
using SupportDesk.Application.Tickets;
using SupportDesk.Domain;

namespace SupportDesk.Controllers;

[ApiController]
[Route("api/tickets")]
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
            var tickets = await _ticketService.GetAllTickets(
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
        return await ExecuteTicketAction(() => _ticketService.GetTicketById(ticketId: id, cancellationToken: cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketService.CreateTicket(request: request, cancellationToken: cancellationToken);

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
    public async Task<ActionResult<TicketResponse>> AssignTicket(int id, AssignTicketRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.AssignTicket(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<TicketResponse>> StartTicket(int id, StartProgressRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.StartProgressTicket(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<ActionResult<TicketResponse>> ResolveTicket(int id, ResolveTicketRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.ResolveTicket(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<TicketResponse>> CloseTicket(int id, CloseTicketRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.CloseTicket(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<TicketResponse>> CancelTicket(int id, CancelTicketRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.CancelTicket(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<TicketCommentResponse>>> GetCommentsForTicket(int id, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.GetComments(ticketId: id, cancellationToken: cancellationToken));
    }
    
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateCommentForTicket(int id, AddCommentRequest request, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.AddComment(ticketId: id, request: request, cancellationToken: cancellationToken));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyCollection<TicketHistoryItemResponse>>> GetHistoryForTicket(int id, CancellationToken cancellationToken)
    {
        return await ExecuteTicketAction(() => _ticketService.GetHistory(ticketId: id, cancellationToken: cancellationToken));
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