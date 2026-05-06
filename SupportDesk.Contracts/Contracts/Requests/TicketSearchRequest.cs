using SupportDesk.Domain;

namespace SupportDesk.Contracts.Requests;

public sealed class TicketSearchRequest
{
    public TicketStatus[] Status { get; set; } = [];
    public TicketPriority? Priority { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public TicketSortBy SortBy { get; set; } = TicketSortBy.CreatedAt;
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}