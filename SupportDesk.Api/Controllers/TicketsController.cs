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
    public async Task<ActionResult<IReadOnlyCollection<TicketListItemResponse>>> GetAllTickets()
    {
        var tickets = await _ticketService.GetAllTickets();
        return Ok(tickets);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> GetTicket(int id)
    {
        return await ExecuteTicketAction(() => _ticketService.GetTicketById(ticketId: id));
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request)
    {
        try
        {
            var ticket = await _ticketService.CreateTicket(request: request);

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
    public async Task<ActionResult<TicketResponse>> AssignTicket(int id, AssignTicketRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.AssignTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/start")]
    public async Task<ActionResult<TicketResponse>> StartTicket(int id, StartProgressRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.StartProgressTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<ActionResult<TicketResponse>> ResolveTicket(int id, ResolveTicketRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.ResolveTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<TicketResponse>> CloseTicket(int id, CloseTicketRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.CloseTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<TicketResponse>> CancelTicket(int id, CancelTicketRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.CancelTicket(ticketId: id, request: request));
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<TicketCommentResponse>>> GetCommentsForTicket(int id)
    {
        return await ExecuteTicketAction(() => _ticketService.GetComments(ticketId: id));
    }
    
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentResponse>> CreateCommentForTicket(int id, AddCommentRequest request)
    {
        return await ExecuteTicketAction(() => _ticketService.AddComment(ticketId: id, request: request));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyCollection<TicketHistoryItemResponse>>> GetHistoryForTicket(int id)
    {
        return await ExecuteTicketAction(() => _ticketService.GetHistory(ticketId: id));
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