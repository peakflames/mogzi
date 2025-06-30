<task name="Start New Task">

<task_objective>
Prepare an AI model to work effectively on the MaxBot project by ensuring comprehensive understanding of the project architecture, design patterns, and implementation details, then gather the user's task objective. The expected output is a fully prepared AI model ready to work productively within established MaxBot patterns.
</task_objective>

<detailed_sequence_steps>

# Start New Task Process - Detailed Sequence of Steps

## 1. Project Context Preparation

1. Use the `read_file` tool to read `docs/process/01_concept_of_operation.md` to understand the project's purpose and operational concepts.

2. Use the `read_file` tool to read `docs/process/02_operational_requirements.md` to understand the functional and technical requirements.

3. Use the `read_file` tool to read `docs/process/03_architecture.md` to understand the system architecture, patterns, and component relationships.

4. Use the `read_file` tool to read `docs/process/04_design.md` to understand the detailed implementation design and coding patterns.

5. Use the `read_file` tool to read `docs/process/05_ai_tool_op_requirements.md` to understand the AI tool operational requirements.

## 2. Codebase Familiarization

1. Use the `read_file` tool to examine the solution structure by reading `src/MaxBot.sln`.

2. Use the `read_file` tool to review the main project dependencies in `src/MaxBot/MaxBot.csproj`.

3. Use the `read_file` tool to understand the global imports in `src/MaxBot/GlobalUsings.cs`.

4. Use the `list_files` tool to analyze the core domain models in `src/MaxBot/Domain/`.

5. Use the `list_files` tool to review the service layer in `src/MaxBot/Services/`.

6. Use the `list_files` tool to examine the tool implementations in `src/MaxBot/Tools/`.

## 3. Current Implementation Status

1. Use the `read_file` tool to check `docs/project_plan.md` for current implementation status and priorities.

2. Analyze which features are complete, in progress, or planned based on the project plan.

3. Note any specific areas of focus or recent changes mentioned in the documentation.

## 4. Key Information Retention

1. Retain understanding of architecture patterns:
    - Dependency Injection with Microsoft.Extensions.DI throughout
    - Factory Pattern for ChatClient creation with Result<T> error handling
    - Strategy Pattern for fuzzy matching in diff operations
    - Functional Error Handling with FluentResults pattern
    - Streaming Architecture using IAsyncEnumerable for real-time updates

2. Retain understanding of technology stack:
    - .NET 9 with C# and AOT compilation
    - Spectre.Console for terminal UI
    - Microsoft.Extensions.AI for AI integration
    - FluentResults for error handling
    - OpenAI SDK for AI services
    - SharpToken for token counting

3. Retain understanding of security model:
    - Working directory enforcement for all file operations
    - Tool approval system (readonly/all modes)
    - Path validation and permission checking
    - Structured XML tool responses

4. Retain understanding of project structure:
    - MaxBot: Core library with domain, services, and tools
    - MaxBot.TUI: Terminal user interface application
    - MaxBot.PawPrints: Terminal interface abstraction
    - MaxBot.Tests: Unit and integration tests

5. Retain understanding of development practices:
    - Test-driven development with comprehensive test coverage
    - Requirements traceability with verification methods
    - Security-first approach with approval mechanisms
    - Cross-platform compatibility (Windows, macOS, Linux)

## 5. Task Objective Gathering

1. Use the `ask_followup_question` tool to ask the user: "I have reviewed the MaxBot project documentation and codebase. I understand the architecture, design patterns, and development practices. What is your objective for today's task?"

2. Wait for the user's response to understand their specific task objective.

3. Confirm understanding of the task objective and any specific requirements or constraints.

## 6. Carry out the request task

1. Carry out servicing the user's task per your system instructions.

</detailed_sequence_steps>

</task>
