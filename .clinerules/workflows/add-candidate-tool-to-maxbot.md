<task name="Add Candidate Tool to MaxBot">

<task_objective>
Integrate a selected AI tool function from the outputs/candidate-tools-impl directory into the MaxBot codebase by implementing the full logic based on equivalent TypeScript tools, registering it with the ChatClient, and enabling user testing through the MaxBot.TUI CLI application. No MCP servers are required. The expected output is updated source code files and a functional tool accessible via the MaxBot.TUI CLI application.
</task_objective>

<detailed_sequence_steps>

# Add Candidate Tool to MaxBot Process - Detailed Sequence of Steps

## 1. Select and Analyze Candidate Tool

1. Use the `list_files` tool to display all available candidate tools in the outputs/candidate-tools-impl/ directory.

2. Use the `ask_followup_question` tool to prompt the user to select which tool they want to integrate, providing the list of available tools as options.

3. Use the `read_file` tool to examine the selected candidate tool's stub implementation.

4. Analyze the tool's structure, including:
    - Constructor parameters and dependencies
    - Method signatures and parameter descriptions
    - TODO comments indicating required implementation
    - AIFunctionFactory configuration (name, description)

## 2. Reference TypeScript Implementation

1. Use the `list_files` tool to explore the tmp/gemini-cli/packages/core/src/tools directory structure.

2. Use the `search_files` tool to locate the equivalent TypeScript tool implementation by searching for the tool name or similar functionality.

3. Use the `read_file` tool to examine the TypeScript implementation, focusing on:
    - Core logic and algorithms
    - Error handling patterns
    - Input validation approaches
    - Output formatting and return values

4. Document key implementation details that need to be translated to C#, including:
    - Security validation patterns
    - File system operations
    - Cross-platform compatibility considerations

## 3. Implement Full Tool Logic

1. Use the `read_file` tool to examine existing MaxBot tools (SystemTools.cs, DiffPatchTools.cs) to understand:
    - MaxBot coding patterns and conventions
    - Security boundary implementations
    - Format responses in a format similar to Gemini's tool implementation added xml tag where applicable
    - Error handling using FluentResults

2. **CRITICAL**: Create the new tool implementation in `src/MaxBot/Tools/` directory, NOT in the candidate tools directory:
    - Use the `write_to_file` tool to create a new file in `src/MaxBot/Tools/[ToolName].cs`
    - Do NOT modify the original candidate file in `outputs/candidate-tools-impl/`
    - The candidate file serves as a reference template only

3. Implement the complete functionality in the new tool file:
    - Implement all TODO items from the candidate tool
    - Add proper input validation and security checks
    - Follow MaxBot patterns for tool approvals and working directory validation
    - Implement response format similar to Gemini's tool implementation added xml tag where applicable
    - Add cross-platform compatibility using RuntimeInformation

4. Ensure the implementation follows developer guidelines:
    - Use file-scoped namespaces
    - Use var keyword for local variables
    - Use async/await patterns
    - Include proper error handling with try-catch blocks

## 4. Register Tool with ChatClient

1. Use the `read_file` tool to examine the current ChatClient.cs constructor to understand the tool registration pattern.

2. Use the `replace_in_file` tool to add the new tool to the ChatClient:
    - Add private property for the new tool class
    - Initialize the tool in the constructor with config and callback parameters
    - Add the tool's functions to the allTools collection using GetTool() or GetTools() method

3. **IMPORTANT**: When adding tools to the collection, be aware of type compatibility:
    - Tools that return `AIFunction` (single tool): Use `allTools.Add(ToolName.GetTool())`
    - Tools that return `List<AIFunction>` (multiple tools): Use `allTools.AddRange(ToolName.GetTools().Cast<AITool>())`
    - Do NOT use `.Cast<AITool>()` on single `AIFunction` returns - this will cause compilation errors

4. Verify the tool is properly added to ChatOptions.Tools collection.

## 5. Build and Test Integration

1. Use the `execute_command` tool to build the solution:
    ```
    dotnet build src/MaxBot.sln
    ```

2. Check for compilation errors and resolve any issues:
    - Use `read_file` to examine error details if build fails
    - Use `replace_in_file` to fix any compilation issues
    - Repeat build process until successful

3. Use the `execute_command` tool to run existing tests to ensure no regressions:
    ```
    dotnet test src/MaxBot.sln
    ```

## 6. User Acceptance Testing

1. **Recommended**: Run the MaxBot.TUI application directly for acceptance testing:
    ```
    dotnet run --project src/Maxbot.TUI/MaxBot.TUI.csproj
    ```
    This avoids the need to publish and is faster for development testing.

2. **Alternative**: Publish the MaxBot.TUI application if you need a standalone executable:
    ```
    dotnet publish ./src/Maxbot.TUI/MaxBot.TUI.csproj -o ./dist
    cd dist && ./maxbot-ui
    ```

3. Use the `ask_followup_question` tool to guide the user through acceptance testing the new tool:
    - Provide instructions for running the MaxBot.TUI CLI
    - Suggest specific test scenarios for the new tool
    - Ask the user to verify the tool functions as expected

4. **Manual Acceptance Testing Process**:
    - User should run the application using one of the methods above
    - While the application runs in its continuous chat loop, ask the AI Model to perform tasks that will trigger the new tool
    - Test scenarios to try:
      - Basic file reading: "Please read the README.md file"
      - Range reading: "Read lines 5-10 from the LICENSE.md file"
      - Error handling: "Try to read a file that doesn't exist"
      - Security testing: "Try to read a file outside the working directory"

3. If issues are discovered during testing:
    - Use `read_file` to examine error logs or output
    - Use `replace_in_file` to make necessary corrections
    - Repeat build and test cycle as needed

4. Use the `attempt_completion` tool to present the final results, including:
    - Summary of the integrated tool and its capabilities
    - Location of the new tool file in the codebase
    - Confirmation that the tool is registered and functional
    - Instructions for ongoing usage and testing

## Common Mistakes and Troubleshooting

### File Location Mistakes
- **MISTAKE**: Modifying the candidate file in `outputs/candidate-tools-impl/` instead of creating a new implementation
- **SOLUTION**: Always create new files in `src/MaxBot/Tools/` directory using `write_to_file`

### Tool Registration Mistakes
- **MISTAKE**: Using `.Cast<AITool>()` on single `AIFunction` returns
- **SOLUTION**: Check the return type first:
  - Single tool (`AIFunction`): `allTools.Add(tool.GetTool())`
  - Multiple tools (`List<AIFunction>`): `allTools.AddRange(tool.GetTools().Cast<AITool>())`

### Build Error Troubleshooting
- **Common Error**: "does not contain a definition for 'Cast'"
- **Cause**: Attempting to cast a single `AIFunction` to `AITool`
- **Fix**: Remove the `.Cast<AITool>()` call for single function tools

### Security Implementation Checklist
- ✅ Implement `IsPathInWorkingDirectory()` validation
- ✅ Use `IWorkingDirectoryProvider` for directory access
- ✅ Add cross-platform path handling with `RuntimeInformation`
- ✅ Include proper exception handling for file operations
- ✅ Validate input parameters before processing

### Response Format Requirements
- ✅ Use structured XML responses with `<tool_response>` tags
- ✅ Include SHA256 checksums for file operations
- ✅ Provide absolute paths in response metadata
- ✅ Use `SecurityElement.Escape()` for XML content
- ✅ Include detailed error messages in `<error>` tags

## Lessons Learned and Critical Tips

### 1. Gemini CLI Compatibility is ESSENTIAL

**CRITICAL**: Always match Gemini CLI tool and parameter descriptions EXACTLY. This ensures consistent behavior and user experience across both systems.

- **Tool Names**: Must match exactly (e.g., `read_file`, `list_directory`)
- **Tool Descriptions**: Copy verbatim from TypeScript implementation
- **Parameter Names**: Use exact names (e.g., `absolute_path`, not `path`)
- **Parameter Descriptions**: Copy verbatim, including examples and formatting
- **Path Handling**: Both systems expect absolute paths, not relative paths

**Example Issue**: Initially implemented tools with relative path handling, but Gemini CLI expects absolute paths. This caused confusion for AI models switching between systems.

### 2. Path Validation Logic Pitfalls

**CRITICAL BUG**: The `IsPathInWorkingDirectory()` method has a subtle but critical flaw that prevents listing/reading the working directory itself.

**The Problem**:
```csharp
// BROKEN: This fails when path IS the working directory
if (!normalizedWorkingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
{
    normalizedWorkingDirectory += Path.DirectorySeparatorChar;
}
return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory, ...);
```

**The Fix**:
```csharp
// CORRECT: Check exact match first, then subdirectories
if (string.Equals(normalizedAbsolutePath, normalizedWorkingDirectory, ...))
{
    return true; // Allow exact working directory match
}
// Then check subdirectories with trailing separator
```

**Symptoms**: AI models report "Path must be within the root directory" when trying to list the current working directory, forcing them to use shell commands instead.

### 3. Parameter Name Consistency

**CRITICAL**: When changing parameter names (e.g., `path` to `absolute_path`), update ALL references:
- Method parameter declarations
- Variable usage throughout the method
- Error messages and logging
- Method calls and responses

**Common Mistake**: Changing the parameter name but forgetting to update variable references in catch blocks or response methods.

### 4. Tool Description Accuracy

**CRITICAL**: Tool descriptions must accurately reflect actual capabilities:
- If tool supports images/PDFs, mention it in description
- If tool requires absolute paths, state it clearly
- Include specific examples in parameter descriptions
- Match the exact wording from Gemini CLI for consistency

### 5. Security Validation Best Practices

**Path Security Checklist**:
- ✅ Validate paths are absolute using `Path.IsPathRooted()`
- ✅ Allow access to the working directory itself (not just subdirectories)
- ✅ Use cross-platform path comparison with `RuntimeInformation`
- ✅ Normalize paths with `Path.GetFullPath()` before comparison
- ✅ Handle both directory separators (`/` and `\`) properly

### 6. Error Message Quality

**Best Practices**:
- Include the actual path in error messages for debugging
- Use consistent error message format across tools
- Provide actionable guidance (e.g., "You must provide an absolute path")
- Match error message style from Gemini CLI when possible

### 7. Testing Strategy

**Integration Testing Approach**:
1. **Build First**: Always ensure compilation succeeds before testing
2. **Run Tests**: Verify no regressions in existing functionality
3. **Manual Testing**: Test with actual AI model interactions
4. **Edge Cases**: Test boundary conditions (working directory, non-existent paths, etc.)

**Common Test Scenarios**:
- List/read the working directory itself
- List/read subdirectories
- Handle non-existent paths gracefully
- Test with paths outside working directory (should fail)
- Test with malformed paths

### 8. AI Model User Experience

**Key Insights from Real Usage**:
- AI models expect tools to "just work" like Gemini CLI
- Confusing error messages lead to workarounds (using shell commands)
- Consistent parameter names reduce model confusion
- Clear, specific error messages help models understand limitations

### 9. Development Workflow Tips

**Efficient Development Process**:
1. **Read TypeScript First**: Always examine the Gemini CLI implementation thoroughly
2. **Copy Descriptions Exactly**: Don't paraphrase - copy verbatim
3. **Test Early**: Build and test after each major change
4. **Fix Security Last**: Get basic functionality working, then add security validation
5. **Test with AI**: Use actual AI model interactions to validate user experience

### 10. Common Pitfalls to Avoid

**File Location Mistakes**:
- ❌ Modifying candidate files instead of creating new implementations
- ❌ Placing tools in wrong directory structure

**Type System Mistakes**:
- ❌ Using `.Cast<AITool>()` on single `AIFunction` returns
- ❌ Mismatching return types in tool registration

**Path Handling Mistakes**:
- ❌ Assuming relative paths when tools expect absolute paths
- ❌ Blocking access to working directory itself
- ❌ Platform-specific path handling

**Description Mistakes**:
- ❌ Paraphrasing instead of copying exact descriptions
- ❌ Missing parameter examples or formatting
- ❌ Inconsistent terminology between tools

</detailed_sequence_steps>

</task>
