using Microsoft.Agents.AI.Workflows;

namespace AgentWorkflowAITicketSupport.Executors;

[SendsMessage(typeof(SupportTicket))]
[YieldsOutput(typeof(string))]
public sealed class ValidateExecutor() : Executor<SupportTicket> ("Validate")
{
    private const int MinLength = 10;

    public override async ValueTask HandleAsync(SupportTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var message = ticket.Message?.Trim() ?? string.Empty;

        if (message.Length < MinLength)
        {
            var reason = message.Length == 0 ? "ticket is empty" : $"ticket is too short (minimum {MinLength} characters)";
            await context.YieldOutputAsync($"Rejected: {reason}.", cancellationToken);
            return;
        }

        await context.SendMessageAsync(new SupportTicket(message), cancellationToken: cancellationToken);
    }
}
