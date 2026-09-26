using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentWorkflowAITicketSupport.Executors;

public sealed class ClassifyExecutor : Executor<SupportTicket, ClassifiedTicket>
{
    private readonly AIAgent _agent;

    public ClassifyExecutor(AIAgent agent) : base("Classify")
    {
        _agent = agent;
    }

    public override async ValueTask<ClassifiedTicket> HandleAsync(SupportTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var response = await _agent.RunAsync<TicketClassification>(ticket.Message, cancellationToken: cancellationToken);

        return new ClassifiedTicket(ticket.Message, response.Result);
    }
}
