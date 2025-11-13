using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using storyteller_chat.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var openAIClient = new OpenAIClient(new ApiKeyCredential("<unused>"), new OpenAIClientOptions { Endpoint = new("https://dotnet-storyteller.azurewebsites.net/publisher/v1/") });
var chatClient = openAIClient.GetChatClient("<unused>").AsIChatClient();
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
