using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// You will need to set the token to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set "GITHUB_TOKEN" "your-github-models-token-here"
var chatClient = new ChatClient(
        "gpt-4o-mini",
        new ApiKeyCredential(builder.Configuration["GITHUB_TOKEN"] ?? throw new InvalidOperationException("Missing configuration: GITHUB_TOKEN")),
        new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") })
    .AsIChatClient();

builder.Services.AddChatClient(chatClient);

builder.AddAIAgent("writer", "Given a topic, you find a way to make it a scenario for using .NET. You achieve this in 200-300 words.");

builder.AddAIAgent("editor", (sp, key) => new ChatClientAgent(
    chatClient,
    name: key,
    instructions: "Given stories that describe how useful .NET can be, you need to edit the story to add how Aspire (previously known as .NET Aspire) can even further the greatness. But you make sure the story stays focused on the original theme, and you keep it to 300 words total. Respond with your edited story, giving it a title and using the tool to format it.",
    tools: [AIFunctionFactory.Create(FormatStory)]
));

var publisher = builder.AddWorkflow("publisher", (sp, key) => AgentWorkflowBuilder.BuildSequential(
    workflowName: key,
    sp.GetRequiredKeyedService<AIAgent>("writer"),
    sp.GetRequiredKeyedService<AIAgent>("editor")
)).AddAsAIAgent();

// Register services for OpenAI responses and conversations (also required for DevUI)
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
builder.Services.AddOpenAIChatCompletions();

var app = builder.Build();
app.UseHttpsRedirection();

// Map endpoints for OpenAI responses and conversations (also required for DevUI)
app.MapOpenAIResponses(publisher);
app.MapOpenAIConversations();
app.MapOpenAIChatCompletions(publisher);

if (builder.Environment.IsDevelopment())
{
    // Map DevUI endpoint to /devui
    app.MapDevUI();
}

app.MapGet("/chat", async context =>
{
    var publisherAgent = app.Services.GetRequiredKeyedService<AIAgent>("publisher");
    var agentResponse = await publisherAgent.RunAsync(context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "Writing intelligent apps");
    await context.Response.WriteAsync(agentResponse.Messages.LastOrDefault()?.Contents.LastOrDefault()?.ToString() ?? "No response from agent.");
});

app.MapMethods("/{**catchall}", new[] { "GET", "POST" }, async context =>
{
    var url = context.Request.Path + context.Request.QueryString;
    if (context.Request.Method == "POST")
    {
        context.Request.EnableBuffering(); // allows reading the body multiple times
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        await context.Response.WriteAsync($"Echoed URL: {url}\nEchoed Body: {body}");
    }
    else
    {
        await context.Response.WriteAsync($"Echoed URL: {url}");
    }
});

app.Run();

[Description("Formats the story for publication, revealing its title.")]
string FormatStory(string title, string story) => $"""
    **Title**: {title}

    {story}
    """;
