namespace LibraryMCP.MCP.Models;

public class McpRequest
{
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}

public class McpResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, ParameterSchema> Parameters { get; set; } = new();
}

public class ParameterSchema
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}
