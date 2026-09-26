using AgentWorkflowAITicketSupport;
using AgentWorkflowAITicketSupport.Executors;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var apiKey = config["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("""OpenAI API key not found. Run: dotnet user-secrets set "OpenAI:ApiKey" "<your-key>" """);
    return;
}

var model = config["OpenAI:Model"] ?? "gpt-4o-mini";
var chatClient = new OpenAIClient(apiKey).GetChatClient(model);

AIAgent classifierAgent = chatClient.AsAIAgent(
    name: "TicketClassifier",
    instructions: """
        You classify customer support tickets for a software company.
        Choose exactly one category:
        - Billing: payments, charges, refunds, invoices, subscriptions, pricing disputes.
        - Technical: bugs, crashes, errors, login problems, performance, how the app behaves.
        - General: everything else, such as sales questions, discounts, partnerships, feedback.
        Also write a one-sentence summary of the customer's issue.
        """);

var validate = new ValidateExecutor();
var classify = new ClassifyExecutor(classifierAgent);

var billingRoute = new RouteExecutor("BillingRoute", "Billing Team", "Verify the charges and process a refund if eligible.");
var technicalRoute = new RouteExecutor("TechnicalRoute", "Engineering Support", "Reproduce the issue and collect logs and the app version.");
var generalRoute = new RouteExecutor("GeneralRoute", "Customer Success", "Answer the question and share relevant resources.");

var workflow = new WorkflowBuilder(validate)
    .AddEdge(validate, classify)
    .AddEdge<ClassifiedTicket>(classify, billingRoute, t => t?.Classification.Category == TicketCategory.Billing)
    .AddEdge<ClassifiedTicket>(classify, technicalRoute, t => t?.Classification.Category == TicketCategory.Technical)
    .AddEdge<ClassifiedTicket>(classify, generalRoute, t => t?.Classification.Category == TicketCategory.General)
    .WithOutputFrom(validate)
    .Build();
