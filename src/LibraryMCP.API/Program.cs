using LibraryMCP.Application;
using LibraryMCP.Infrastructure;
using LibraryMCP.Infrastructure.Persistence;
using LibraryMCP.MCP;
using LibraryMCP.MCP.Models;
using LibraryMCP.MCP.Server;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=library.db";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddMcpServer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.MapGet("/mcp/tools", (McpServerHandler handler) =>
{
    return Results.Ok(handler.GetAvailableTools());
})
.WithName("GetMcpTools")
.WithDescription("Retorna lista de ferramentas disponíveis no servidor MCP");

app.MapPost("/mcp/execute", async (McpRequest request, McpServerHandler handler) =>
{
    var response = await handler.ExecuteToolAsync(request.Method, request.Params);
    return response.Success ? Results.Ok(response) : Results.BadRequest(response);
})
.WithName("ExecuteMcpTool")
.WithDescription("Executa uma ferramenta do servidor MCP");

app.Run();
