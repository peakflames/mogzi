# MCP Tools Specification

## Overview
Model Context Protocol (MCP) tools enable communication with locally running MCP servers that provide additional capabilities beyond the built-in MaxBot tools. These tools extend functionality by connecting to external services, APIs, and specialized resources.

## Available MCP Tools

### use_mcp_tool

**Purpose**: Execute a tool provided by a connected MCP server.

**Parameters**:
- `server_name` (required): The name of the MCP server providing the tool
- `tool_name` (required): The name of the specific tool to execute
- `arguments` (required): JSON object containing the tool's input parameters

**Usage**:
```xml
<use_mcp_tool>
<server_name>weather-server</server_name>
<tool_name>get_forecast</tool_name>
<arguments>
{
  "city": "San Francisco",
  "days": 5
}
</arguments>
</use_mcp_tool>
```

**Capabilities**:
- Execute tools from any connected MCP server
- Pass structured arguments as JSON
- Receive structured responses from external services
- Access specialized functionality not available in built-in tools

### access_mcp_resource

**Purpose**: Access a resource provided by a connected MCP server.

**Parameters**:
- `server_name` (required): The name of the MCP server providing the resource
- `uri` (required): The URI identifying the specific resource to access

**Usage**:
```xml
<access_mcp_resource>
<server_name>file-server</server_name>
<uri>file:///path/to/document.pdf</uri>
</access_mcp_resource>
```

**Capabilities**:
- Access data sources like files, APIs, or system information
- Retrieve structured data for use as context
- Connect to external databases or services
- Access specialized data formats and sources

### load_mcp_documentation

**Purpose**: Load documentation about creating MCP servers.

**Parameters**: None

**Usage**:
```xml
<load_mcp_documentation>
</load_mcp_documentation>
```

**When to Use**:
- User requests to create or install an MCP server
- User asks to "add a tool" for specific functionality
- Need to understand MCP server creation process
- Want to extend MaxBot's capabilities with custom tools

## MCP Server Examples

### GitHub Server
```xml
<use_mcp_tool>
<server_name>github.com/modelcontextprotocol/servers/tree/main/src/github</server_name>
<tool_name>create_issue</tool_name>
<arguments>
{
  "owner": "octocat",
  "repo": "hello-world",
  "title": "Found a bug",
  "body": "I'm having a problem with this.",
  "labels": ["bug", "help wanted"],
  "assignees": ["octocat"]
}
</arguments>
</use_mcp_tool>
```

### Weather Server
```xml
<use_mcp_tool>
<server_name>weather-api</server_name>
<tool_name>get_current_weather</tool_name>
<arguments>
{
  "location": "New York, NY",
  "units": "metric"
}
</arguments>
</use_mcp_tool>
```

### Database Server
```xml
<use_mcp_tool>
<server_name>database-connector</server_name>
<tool_name>execute_query</tool_name>
<arguments>
{
  "query": "SELECT * FROM users WHERE active = true",
  "database": "production"
}
</arguments>
</use_mcp_tool>
```

## Best Practices

### Tool Usage
- Use one MCP tool per message, similar to built-in tools
- Wait for confirmation of success before proceeding
- Provide proper JSON formatting for arguments
- Handle errors and unexpected responses gracefully

### Server Selection
- Choose appropriate MCP server for the task
- Understand server capabilities before using tools
- Use server names exactly as configured
- Verify server is connected and available

### Argument Formatting
- Follow the tool's input schema exactly
- Use proper JSON syntax with correct data types
- Include all required parameters
- Validate argument structure before sending

## Common Use Cases

### External API Integration
- Weather data retrieval
- Social media posting
- Email sending
- Payment processing
- Cloud service management

### Data Access
- Database queries
- File system access beyond local directory
- Remote file retrieval
- API data fetching
- System information gathering

### Specialized Operations
- Image processing
- Document conversion
- Code analysis
- Security scanning
- Performance monitoring

## Error Handling
- MCP servers may be unavailable or disconnected
- Tools may fail due to network issues or API limits
- Invalid arguments will cause tool execution failures
- Server responses may contain error information

## Integration with Built-in Tools
- Use MCP tools when built-in tools are insufficient
- Combine MCP tools with file operations for data processing
- Use browser_action to verify results from web-based MCP tools
- Leverage search_files to find integration points for MCP data

## Server Management
- Servers are configured externally to MaxBot
- Server availability depends on local setup
- Server names are unique identifiers
- Multiple servers can provide similar functionality

## Security Considerations
- MCP servers may access external networks
- Sensitive data may be transmitted to external services
- Authentication credentials may be required
- Consider privacy implications of external tool usage

## Performance Considerations
- MCP tools may have slower response times than built-in tools
- Network connectivity affects MCP tool performance
- Some tools may have rate limits or usage quotas
- Complex operations may require multiple tool calls

## Creating Custom MCP Servers
When users request functionality not available in existing tools:
1. Use `load_mcp_documentation` to understand the process
2. Guide user through MCP server creation
3. Help configure the server for their specific needs
4. Test the new server tools once implemented
