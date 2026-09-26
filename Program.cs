using AgentWorkflowAITicketSupport.Executors;
using Microsoft.Agents.AI;
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
