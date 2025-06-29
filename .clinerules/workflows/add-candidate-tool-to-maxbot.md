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

2. Use the `replace_in_file` tool to replace the stub implementation with complete functionality:
    - Implement all TODO items from the candidate tool
    - Add proper input validation and security checks
    - Follow MaxBot patterns for tool approvals and working directory validation
    - Implement response format similar to Gemini's tool implementation added xml tag where applicable
    - Add cross-platform compatibility using RuntimeInformation

3. Ensure the implementation follows developer guidelines:
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

3. Verify the tool is properly added to ChatOptions.Tools collection.

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

1. Use the `execute_command` tool to publish the MaxBot.TUI application:
    ```
    dotnet publish ./src/Maxbot.TUI/MaxBot.TUI.csproj -o ./dist
    ```

2. Use the `ask_followup_question` tool to guide the user through testing the new tool:
    - Provide instructions for running the MaxBot.TUI CLI
    - Suggest specific test scenarios for the new tool
    - Ask the user to verify the tool functions as expected

3. If issues are discovered during testing:
    - Use `read_file` to examine error logs or output
    - Use `replace_in_file` to make necessary corrections
    - Repeat build and test cycle as needed

4. Use the `attempt_completion` tool to present the final results, including:
    - Summary of the integrated tool and its capabilities
    - Location of the new tool file in the codebase
    - Confirmation that the tool is registered and functional
    - Instructions for ongoing usage and testing

</detailed_sequence_steps>

</task>
