using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AgentWorkflowAITicketSupport;

[JsonConverter(typeof(JsonStringEnumConverter<TicketCategory>))]
public enum TicketCategory
{
    Billing,
    Technical,
    General
}

public record SupportTicket(string Message);

public record TicketClassification(
    [property: Description("The ticket category: Billing, Technical, or General.")] TicketCategory Category,
    [property: Description("A one-sentence summary of the customer's issue.")] string Summary);

public record ClassifiedTicket(string Message, TicketClassification Classification);

public record RoutedTicket(
    string Message,
    TicketCategory Category,
    string Summary,
    string AssignedTeam,
    string HandlingInstruction);

public record TicketResult(
    TicketCategory Category,
    string AssignedTeam,
    string Summary,
    string DraftResponse);
