using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace AgentWorkflowAITicketSupport.Executors;

[YieldsOutput(typeof(TicketResult))]
public sealed class DraftResponseExecutor : Executor<RoutedTicket>
{
    private readonly AIAgent _agent;

    public DraftResponseExecutor(AIAgent agent) : base("DraftResponse")
    {
        _agent = agent;
    }

    public override async ValueTask HandleAsync(RoutedTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Customer message: {ticket.Message}
            Category: {ticket.Category}
            Summary: {ticket.Summary}
            Assigned team: {ticket.AssignedTeam}
            Internal handling instruction: {ticket.HandlingInstruction}
            """;

        var response = await _agent.RunAsync(prompt, cancellationToken: cancellationToken);

        var result = new TicketResult(ticket.Category, ticket.AssignedTeam, ticket.Summary, response.Text.Trim());
        await context.YieldOutputAsync(result, cancellationToken);
    }
}
