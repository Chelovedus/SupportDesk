using FluentAssertions;
using SupportDesk.Domain;

namespace SupportDesk.UnitTests.Domain;

public class TicketTests
{
    [Fact]
    public void New_ticket_should_have_new_status()
    {
        // Arrange
        var ticket = CreateTicket();

        // Assert
        ticket.Status.Should().Be(TicketStatus.New);
    }
    [Fact]
    public void New_ticket_can_be_assigned()
    {
        // Arrange
        var ticket = CreateTicket();

        // Act
        ticket.AssignTo(agentId: 3, actorId: 1);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Assigned);
    }
    
    [Fact]
    public void AssignTo_should_set_assigned_agent_id()
    {
        // Arrange
        var ticket = CreateTicket();
        var assignedAgentId = 6;
        
        ticket.AssignTo(agentId: assignedAgentId, actorId: 1);
        
        ticket.AssignedAgentId.Should().Be(assignedAgentId);
    }
    
    [Fact]
    public void New_ticket_cannot_be_closed()
    {
        var ticket = CreateTicket();

        var act = () => ticket.Close(actorId: 4);
        
        act.Should().Throw<DomainException>();
        ticket.Status.Should().Be(TicketStatus.New);
        ticket.ClosedAt.Should().BeNull();
    }
    
    [Fact]
    public void Assigned_ticket_can_start_progress()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: 4, agentId: 1);
        ticket.StartProgress(actorId: 4);
        
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }
    
    [Fact]
    public void InProgress_ticket_can_be_resolved()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: 4, agentId: 1);
        ticket.StartProgress(actorId: 4);
        ticket.Resolve(actorId: 4, "User was unlocked.");
        
        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Resolved_ticket_can_be_closed()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: 4, agentId: 1);
        ticket.StartProgress(actorId: 4);
        ticket.Resolve(actorId: 4, "User was unlocked.");
        ticket.Close(actorId: 4);

        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Closed_ticket_cannot_be_changed()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: 4, agentId: 1);
        ticket.StartProgress(actorId: 4);
        ticket.Resolve(actorId: 4, "User was unlocked.");
        ticket.Close(actorId: 4);
        
        // ReSharper disable ConvertToLocalFunction
        var actAssign = () => ticket.AssignTo(agentId: 1, actorId: 4);
        var actStartProgress = () => ticket.StartProgress(actorId: 4);
        var actResolve = () => ticket.Resolve(actorId: 4, "Again unlocked.");
        var actCancel = () => ticket.Cancel(actorId: 4, reason: "User deleted.");
        // ReSharper restore ConvertToLocalFunction
        Action[] actions = [actAssign, actResolve, actStartProgress, actCancel];

        foreach (var action in actions)
        {
            action.Should().Throw<DomainException>();
        }
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
    }
        
    private static Ticket CreateTicket()
    {
        var ticket = new Ticket(
            id: 1,
            title: "I cannot login",
            description: "Exception: password incorrect. Help me!!",
            createdByUserId: 1,
            priority: TicketPriority.Normal);
        
        return ticket;
    }
}