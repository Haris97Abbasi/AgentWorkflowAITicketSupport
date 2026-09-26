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

AIAgent responseAgent = chatClient.AsAIAgent(
    name: "ResponseWriter",
    instructions: """
        You write replies to customer support tickets for a software company.
        Write a concise, professional and empathetic reply of 3-5 sentences, addressed to the customer.
        Mention which team is handling the ticket and what happens next, based on the handling instruction.
        Do not promise refunds, fixes or timelines that you cannot guarantee.
        Do not reveal internal wording verbatim, and do not add a subject line or placeholders like [Name].
        """);

var validate = new ValidateExecutor();
var classify = new ClassifyExecutor(classifierAgent);
var draft = new DraftResponseExecutor(responseAgent);

var billingRoute = new RouteExecutor("BillingRoute", "Billing Team", "Verify the charges and process a refund if eligible.");
var technicalRoute = new RouteExecutor("TechnicalRoute", "Engineering Support", "Reproduce the issue and collect logs and the app version.");
var generalRoute = new RouteExecutor("GeneralRoute", "Customer Success", "Answer the question and share relevant resources.");

var workflow = new WorkflowBuilder(validate)
    .AddEdge(validate, classify)
    .AddEdge<ClassifiedTicket>(classify, billingRoute, t => t?.Classification.Category == TicketCategory.Billing)
    .AddEdge<ClassifiedTicket>(classify, technicalRoute, t => t?.Classification.Category == TicketCategory.Technical)
    .AddEdge<ClassifiedTicket>(classify, generalRoute, t => t?.Classification.Category == TicketCategory.General)
    .AddEdge(billingRoute, draft)
    .AddEdge(technicalRoute, draft)
    .AddEdge(generalRoute, draft)
    .WithOutputFrom(validate, draft)
    .Build();

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("AI Support Ticket Workflow. Type a support message and press Enter (or 'exit' to quit).");

while (true)
{
    Console.WriteLine();
    Console.Write("Ticket> ");
    var input = Console.ReadLine();

    if (input is null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await using var run = await InProcessExecution.RunStreamingAsync(workflow, new SupportTicket(input));

    object? output = null;

    await foreach (var evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case ExecutorCompletedEvent completed:
                Console.WriteLine($"  ✓ {completed.ExecutorId}");
                break;

            case WorkflowOutputEvent outputEvent:
                output = outputEvent.Data;
                break;

            case ExecutorFailedEvent failed:
                Console.WriteLine($"  ✗ {failed.ExecutorId} failed: {failed.Data}");
                break;

            case WorkflowErrorEvent error:
                Console.WriteLine($"Workflow error: {error.Exception?.Message}");
                break;
        }
    }

    switch (output)
    {
        case string rejection:
            Console.WriteLine();
            Console.WriteLine(rejection);
            break;

        case TicketResult result:
            PrintResult(result);
            break;
    }
}

static void PrintResult(TicketResult result)
{
    Console.WriteLine();
    Console.WriteLine("=== Ticket Result ===");
    Console.WriteLine($"Category:      {result.Category}");
    Console.WriteLine($"Assigned team: {result.AssignedTeam}");
    Console.WriteLine($"Summary:       {result.Summary}");
    Console.WriteLine("Draft reply:");
    Console.WriteLine(result.DraftResponse);
}
