<task name="Align Documentation with Implementation">

<task_objective>
Ensure that all architectural and design patterns and decisions found in the Mogzi implementation are reflected and sufficiently articulated by the corresponding documentation. The analysis should provide enough detail that after an AI Model reads the architecture and design documents, it will be effectively prepared to work in the project. No MCP servers are required. The expected output is updated documentation files that align with the actual source code implementation.
</task_objective>

<detailed_sequence_steps>

# Align Documentation with Implementation Process - Detailed Sequence of Steps

## 1. Analyze Current Implementation

1. Use the `list_files` tool to examine the overall Mogzi project structure, focusing on the `src/` directory (excluding `src/Cli` as it's obsolete).

2. Use the `list_code_definition_names` tool to get an overview of key classes, interfaces, and methods in each major directory:
   - `src/Mogzi/` - Core domain and services
   - `src/Mogzi.PawPrints/` - Terminal interface components
   - `src/Mogzi.TUI/` - Text user interface application

3. Use the `read_file` tool to examine key architectural files:
   - `src/Mogzi/GlobalUsings.cs` - Understanding dependencies and imports
   - `src/Mogzi/Mogzi.csproj` - Project dependencies and framework version
   - `src/Mogzi.sln` - Solution structure and project relationships

4. Analyze the domain layer by reading core domain files:
   - `src/Mogzi/Domain/ApplicationConfiguration.cs` - Configuration patterns
   - `src/Mogzi/Domain/ChatHistory.cs` - Data models and structures
   - `src/Mogzi/Domain/IWorkingDirectoryProvider.cs` - Interface patterns

5. Examine the service layer architecture:
   - `src/Mogzi/Services/AppService.cs` - Main application service patterns
   - `src/Mogzi/Services/ChatHistoryService.cs` - Service implementation patterns
   - `src/Mogzi/ChatClient/ChatClient.cs` - External integration patterns

6. Analyze the tools architecture:
   - Use the `list_files` tool to list all files in `src/Mogzi/Tools/`
   - Use the `read_file` tool to examine 2-3 representative tool implementations
   - Document the tool registration and dependency injection patterns

7. Document key architectural patterns found:
   - Dependency injection patterns and service registration
   - Interface segregation and abstraction patterns
   - Error handling approaches (FluentResults usage)
   - Configuration management patterns
   - Tool integration and registration patterns

## 2. Review Existing Documentation

1. Use the `read_file` tool to thoroughly examine the current architecture documentation:
   - `docs/process/03_architecture.md` - Current architectural decisions and patterns

2. Use the `read_file` tool to thoroughly examine the current design documentation:
   - `docs/process/04_00_design.md` - Current design patterns and implementation details

3. Analyze the documentation structure and identify:
   - What architectural patterns are currently documented
   - What design decisions are explained
   - What implementation details are covered
   - What examples or code snippets are provided
   - What diagrams or visual representations exist

4. Create a comprehensive inventory of documented vs. undocumented elements:
   - List all architectural patterns mentioned in docs
   - List all design patterns mentioned in docs
   - Note any outdated or incorrect information
   - Identify missing cross-references between architecture and design

## 3. Identify Documentation Gaps

1. Compare the implementation analysis (Step 1) against the documentation review (Step 2) to identify:
   - **Missing Architectural Patterns**: Patterns used in code but not documented
   - **Outdated Information**: Documentation that doesn't match current implementation
   - **Insufficient Detail**: Areas where documentation lacks depth for AI model preparation
   - **Missing Examples**: Code patterns that need concrete examples in documentation

2. Create a detailed gap analysis covering:
   - **Service Layer Gaps**: Service registration, dependency injection, lifecycle management
   - **Domain Layer Gaps**: Entity relationships, data flow, business logic patterns
   - **Tool Architecture Gaps**: Tool registration, security patterns, response formatting
   - **Configuration Gaps**: Configuration loading, validation, and usage patterns
   - **Error Handling Gaps**: FluentResults usage, exception handling strategies
   - **Integration Gaps**: External service integration patterns (AI services, file system)

3. Prioritize gaps based on importance for AI model preparation:
   - **Critical**: Core architectural patterns that affect all development work
   - **Important**: Design patterns that affect specific feature development
   - **Helpful**: Implementation details that improve code understanding

4. Use the `search_files` tool to find specific implementation examples for each identified gap:
   - Search for dependency injection patterns
   - Search for FluentResults usage patterns
   - Search for tool registration patterns
   - Search for configuration usage patterns

## 4. Update Architecture Documentation

1. Use the `read_file` tool to re-examine `docs/process/03_architecture.md` in detail.

2. Use the `replace_in_file` tool to update the architecture documentation with missing patterns:
   - **Service Architecture Section**: Add detailed service layer patterns, dependency injection setup, service lifecycle management
   - **Domain Architecture Section**: Add domain model patterns, entity relationships, business logic organization
   - **Tool Architecture Section**: Add tool registration patterns, security boundaries, tool lifecycle
   - **Configuration Architecture Section**: Add configuration loading, validation, and dependency injection patterns
   - **Error Handling Architecture Section**: Add FluentResults patterns, exception handling strategies
   - **Integration Architecture Section**: Add external service integration patterns, AI service communication

3. For each architectural pattern added, include:
   - **Pattern Description**: What the pattern is and why it's used
   - **Implementation Location**: Where in the codebase the pattern is implemented
   - **Key Classes/Interfaces**: Specific types that implement the pattern
   - **Usage Examples**: Brief code snippets showing the pattern in action
   - **Dependencies**: What other patterns or components this pattern relies on

4. Add architectural diagrams or improve existing ones:
   - Service dependency diagram
   - Tool registration flow diagram
   - Configuration loading sequence
   - Error handling flow

5. Ensure cross-references to design documentation are accurate and helpful.

## 5. Update Design Documentation

1. Use the `read_file` tool to re-examine `docs/process/04_00_design.md` in detail.

2. Use the `replace_in_file` tool to update the design documentation with implementation details:
   - **Service Design Section**: Add specific service implementations, method signatures, interaction patterns
   - **Domain Design Section**: Add entity design details, property patterns, validation approaches
   - **Tool Design Section**: Add tool implementation patterns, parameter handling, response formatting
   - **UI Design Section**: Add TUI implementation details, state management, user interaction patterns
   - **Configuration Design Section**: Add configuration schema, validation rules, default values
   - **Error Handling Design Section**: Add specific error types, error propagation, user-facing error messages

3. For each design pattern added, include:
   - **Design Rationale**: Why this design approach was chosen
   - **Implementation Details**: How the design is implemented in code
   - **Code Examples**: Concrete code snippets showing the design in practice
   - **Alternative Approaches**: What other approaches were considered and why they were rejected
   - **Testing Considerations**: How the design supports testing and maintainability

4. Add detailed method signatures and class structures for key components:
   - Core service interfaces and implementations
   - Domain entity structures and relationships
   - Tool base classes and implementation patterns
   - Configuration classes and validation logic

5. Include specific implementation guidance:
   - Coding conventions and patterns used
   - Security considerations and implementation
   - Performance considerations and optimizations
   - Cross-platform compatibility approaches

6. Ensure design documentation provides sufficient detail for:
   - Understanding existing code structure
   - Implementing new features consistently
   - Debugging and troubleshooting issues
   - Extending the system with new components

## 6. Validate Documentation Completeness

1. Use the `read_file` tool to review both updated documentation files to ensure:
   - **Completeness**: All major implementation patterns are documented
   - **Accuracy**: Documentation matches actual implementation
   - **Clarity**: Information is clear and actionable for AI models
   - **Consistency**: Architecture and design docs are aligned and cross-referenced

2. Create a validation checklist covering:
   - **Service Layer Coverage**: All major services and their patterns are documented
   - **Domain Layer Coverage**: All domain entities and business logic patterns are documented
   - **Tool Layer Coverage**: Tool architecture and implementation patterns are documented
   - **Configuration Coverage**: Configuration patterns and usage are documented
   - **Error Handling Coverage**: Error handling strategies are documented
   - **Integration Coverage**: External integration patterns are documented

3. Test documentation completeness by simulating AI model preparation:
   - Review if the documentation provides enough context for understanding the codebase
   - Verify that common development tasks are supported by the documentation
   - Ensure that architectural decisions are explained with sufficient rationale
   - Check that design patterns include enough implementation detail

4. Use the `search_files` tool to verify that documented patterns actually exist in the codebase:
   - Search for classes and interfaces mentioned in documentation
   - Verify that code examples in documentation are accurate
   - Confirm that file paths and references are correct

5. Create a final summary of documentation improvements:
   - List all major additions to architecture documentation
   - List all major additions to design documentation
   - Highlight key patterns that are now properly documented
   - Note any remaining areas that might need future attention

6. Use the `attempt_completion` tool to present a short breifing of the final results, including:
   - Key architectural and design patterns now properly documented
   - Specific areas where documentation was enhanced for AI model preparation
   - Confirmation that implementation and documentation are now aligned

## Common Patterns to Document

### Service Layer Patterns
- **Dependency Injection**: How services are registered and resolved
- **Service Interfaces**: Interface segregation and abstraction patterns
- **Service Lifecycle**: Singleton vs. transient service patterns
- **Service Communication**: How services interact with each other

### Domain Layer Patterns
- **Entity Design**: How domain entities are structured and validated
- **Business Logic**: Where and how business rules are implemented
- **Data Transfer**: How data flows between layers
- **Validation Patterns**: Input validation and business rule validation

### Tool Architecture Patterns
- **Tool Registration**: How tools are discovered and registered
- **Security Boundaries**: How tools enforce security constraints
- **Parameter Handling**: How tool parameters are validated and processed
- **Response Formatting**: How tool responses are structured and returned

### Configuration Patterns
- **Configuration Loading**: How configuration is loaded and validated
- **Environment Handling**: How different environments are supported
- **Default Values**: How default configuration values are managed
- **Configuration Injection**: How configuration is provided to services

### Error Handling Patterns
- **FluentResults Usage**: How success/failure results are handled
- **Exception Handling**: When and how exceptions are used
- **Error Propagation**: How errors flow through the system
- **User-Facing Errors**: How errors are presented to users

### Integration Patterns
- **AI Service Integration**: How external AI services are integrated
- **File System Integration**: How file operations are handled securely
- **Cross-Platform Support**: How platform differences are handled
- **External Dependencies**: How external libraries and services are integrated

## Documentation Quality Checklist

### Architecture Documentation Quality
- ✅ All major architectural patterns are documented
- ✅ Service layer architecture is clearly explained
- ✅ Domain layer organization is documented
- ✅ Tool architecture and security patterns are covered
- ✅ Configuration and dependency injection patterns are explained
- ✅ Error handling architecture is documented
- ✅ Integration patterns with external services are covered
- ✅ Cross-references to design documentation are accurate

### Design Documentation Quality
- ✅ Implementation details for all major components are provided
- ✅ Code examples demonstrate key patterns
- ✅ Method signatures and class structures are documented
- ✅ Design rationale is explained for key decisions
- ✅ Security implementation details are covered
- ✅ Testing and maintainability considerations are addressed
- ✅ Cross-platform implementation details are documented
- ✅ Performance considerations are noted where relevant

### AI Model Preparation Quality
- ✅ Documentation provides sufficient context for understanding the codebase
- ✅ Common development tasks are supported by the documentation
- ✅ Architectural decisions include clear rationale
- ✅ Design patterns include concrete implementation examples
- ✅ Security patterns and constraints are clearly explained
- ✅ Configuration and setup procedures are documented
- ✅ Error handling approaches are clearly described
- ✅ Extension points and customization patterns are documented

</detailed_sequence_steps>

</task>
