using FluentAssertions;
using SupportDesk.Domain;

namespace SupportDesk.UnitTests.Domain;

public class TicketTests
{
    private static readonly Guid UserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid AgentId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ActorId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    
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
        ticket.AssignTo(agentId: AgentId, actorId: ActorId);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Assigned);
    }
    
    [Fact]
    public void AssignTo_should_set_assigned_agent_id()
    {
        // Arrange
        var ticket = CreateTicket();
        var assignedAgentId = Guid.CreateVersion7();
        
        ticket.AssignTo(agentId: assignedAgentId, actorId: ActorId);
        
        ticket.AssignedAgentId.Should().Be(assignedAgentId);
    }
    
    [Fact]
    public void New_ticket_cannot_be_closed()
    {
        var ticket = CreateTicket();

        var act = () => ticket.Close(actorId: ActorId);
        
        act.Should().Throw<DomainException>();
        ticket.Status.Should().Be(TicketStatus.New);
        ticket.ClosedAt.Should().BeNull();
    }
    
    [Fact]
    public void Assigned_ticket_can_start_progress()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: ActorId, agentId: AgentId);
        ticket.StartProgress(actorId: ActorId);
        
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }
    
    [Fact]
    public void InProgress_ticket_can_be_resolved()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: ActorId, agentId: AgentId);
        ticket.StartProgress(actorId: ActorId);
        ticket.Resolve(actorId: ActorId, "User was unlocked.");
        
        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Resolved_ticket_can_be_closed()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: ActorId, agentId: AgentId);
        ticket.StartProgress(actorId: ActorId);
        ticket.Resolve(actorId: ActorId, "User was unlocked.");
        ticket.Close(actorId: ActorId);

        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
    }
    
    [Fact]
    public void Closed_ticket_cannot_be_changed()
    {
        var ticket = CreateTicket();

        ticket.AssignTo(actorId: ActorId, agentId: AgentId);
        ticket.StartProgress(actorId: ActorId);
        ticket.Resolve(actorId: ActorId, "User was unlocked.");
        ticket.Close(actorId: ActorId);
        
        // ReSharper disable ConvertToLocalFunction
        var actAssign = () => ticket.AssignTo(agentId: AgentId, actorId: ActorId);
        var actStartProgress = () => ticket.StartProgress(actorId: ActorId);
        var actResolve = () => ticket.Resolve(actorId: ActorId, "Again unlocked.");
        var actCancel = () => ticket.Cancel(actorId: ActorId, reason: "User deleted.");
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
            title: "I cannot login",
            description: "Exception: password incorrect. Help me!!",
            createdByUserId: UserId,
            priority: TicketPriority.Normal);
        
        return ticket;
    }
}