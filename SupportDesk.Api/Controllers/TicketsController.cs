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
    public ActionResult<IReadOnlyCollection<TicketListItemResponse>> GetAllTickets()
    {
        return Ok(_ticketService.GetAllTickets());
    }
    
    [HttpGet("{id:int}")]
    public ActionResult<TicketResponse> GetTicket(int id)
    {
        return ExecuteTicketAction(() => _ticketService.GetTicketById(ticketId: id));
    }

    [HttpPost]
    public ActionResult<TicketResponse> CreateTicket(CreateTicketRequest request)
    {
        try
        {
            var ticket = _ticketService.CreateTicket(request: request);

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
    public ActionResult<TicketResponse> AssignTicket(int id, AssignTicketRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.AssignTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/start")]
    public ActionResult<TicketResponse> StartTicket(int id, StartProgressRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.StartProgressTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/resolve")]
    public ActionResult<TicketResponse> ResolveTicket(int id, ResolveTicketRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.ResolveTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/close")]
    public ActionResult<TicketResponse> CloseTicket(int id, CloseTicketRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.CloseTicket(ticketId: id, request: request));
    }

    [HttpPost("{id:int}/cancel")]
    public ActionResult<TicketResponse> CancelTicket(int id, CancelTicketRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.CancelTicket(ticketId: id, request: request));
    }

    [HttpGet("{id:int}/comments")]
    public ActionResult<IReadOnlyCollection<TicketCommentResponse>> GetCommentsForTicket(int id)
    {
        return ExecuteTicketAction(() => _ticketService.GetComments(ticketId: id));
    }
    
    [HttpPost("{id:int}/comments")]
    public ActionResult<TicketCommentResponse> CreateCommentForTicket(int id, AddCommentRequest request)
    {
        return ExecuteTicketAction(() => _ticketService.AddComment(ticketId: id, request: request));
    }

    [HttpGet("{id:int}/history")]
    public ActionResult<IReadOnlyCollection<TicketHistoryItemResponse>> GetHistoryForTicket(int id)
    {
        return ExecuteTicketAction(() => _ticketService.GetHistory(ticketId: id));
    }
    
    private ActionResult<TResponse> ExecuteTicketAction<TResponse>(Func<TResponse?> action)
    {
        try
        {
            var response = action();

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