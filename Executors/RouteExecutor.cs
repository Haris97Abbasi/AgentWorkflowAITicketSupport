using Microsoft.Agents.AI.Workflows;

namespace AgentWorkflowAITicketSupport.Executors;

public sealed class RouteExecutor : Executor<ClassifiedTicket, RoutedTicket>
{
    private readonly string _assignedTeam;
    private readonly string _handlingInstruction;

    public RouteExecutor(string id, string assignedTeam, string handlingInstruction) : base(id)
    {
        _assignedTeam = assignedTeam;
        _handlingInstruction = handlingInstruction;
    }

    public override ValueTask<RoutedTicket> HandleAsync(ClassifiedTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var routed = new RoutedTicket(
            ticket.Message,
            ticket.Classification.Category,
            ticket.Classification.Summary,
            _assignedTeam,
            _handlingInstruction);

        return ValueTask.FromResult(routed);
    }
}
