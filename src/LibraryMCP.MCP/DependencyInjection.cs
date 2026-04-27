using LibraryMCP.MCP.Server;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryMCP.MCP;

public static class DependencyInjection
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services)
    {
        services.AddScoped<McpServerHandler>();
        
        return services;
    }
}
