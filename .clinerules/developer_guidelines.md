# Developer Guidelines

This document outlines the coding conventions, rules, and patterns used in this project.

## General Conventions

- **`namespace`:** All code should be within a namespace. Use file-scoped namespaces to reduce nesting.
- **`GlobalUsings.cs`:** Use `GlobalUsings.cs` to define `using` statements that apply to the entire project.
- **`var` keyword:** Use the `var` keyword for local variable declarations.
- **String interpolation:** Use string interpolation for creating formatted strings.
- **`async Task`:** Use `async Task` for asynchronous operations.
- **`try-catch` blocks:** Use `try-catch` blocks for error handling.
- **Comments:** Use single-line (`//`) or multi-line (`/* */`) comments to document code.

## Class Design

- **`class`:** Classes should be `public` unless they are internal to an assembly.
- **`static class`:** Use `static` classes for collections of utility methods.
- **`partial class`:** Use `partial` classes to split large class definitions into multiple files.
- **`record`:** Use `record` types for immutable data transfer objects (DTOs).
- **`init` properties:** Use `init` setters for properties that should be immutable after initialization.
- **POCOs:** Use Plain Old CLR Objects (POCOs) for data structures.

## Error Handling

- **`FluentResults`:** Use the `FluentResults` library to handle success and failure without throwing exceptions. Return `Result<T>` from methods that can fail.

## Asynchronous Programming

- **`async`/`await`:** Use `async` and `await` for non-blocking I/O operations.

## JSON Serialization

- **`System.Text.Json`:** Use the `System.Text.Json` library for JSON serialization and deserialization.
- **`[JsonPropertyName]`:** Use the `[JsonPropertyName]` attribute to map JSON property names to C# property names.
- **`[JsonSerializable]`:** Use the `[JsonSerializable]` attribute and `JsonSerializerContext` for improved serialization performance.

## Dependency Management

- **Builder pattern:** Use the builder pattern for complex object construction.
- **Factory method:** Use static factory methods to create instances of classes.

## LINQ

- **LINQ:** Use LINQ for querying collections.

## Command-line Argument Parsing

- **Manual parsing:** Arguments are parsed manually.
- **`--help` and `--version`:** Provide `--help` and `--version` options for command-line tools.

## Best Practices

- **File-scoped namespaces:** Use file-scoped namespaces to reduce indentation and improve readability.
- **Expression-bodied members:** Use expression-bodied members for simple methods and properties.
- **`IAsyncEnumerable<T>`:** Use `IAsyncEnumerable<T>` for streaming asynchronous data.
- **`nameof` operator:** Use the `nameof` operator to get the name of a variable, type, or member as a string.

## Testing

- **Black-box testing:** Prefer black-box testing for integration tests.
- **Invoke `Program.Main`:** Invoke the `Program.Main` method directly from tests to simulate running the CLI.
- **Redirect Console I/O:** Use `Console.SetIn` and `Console.SetOut` to redirect console input and output for testing.
- **Test `ChatClient`:** Create a test implementation of `ChatClient` to return predictable responses.

## Package Management

- **NuGet dependencies:** Be mindful of NuGet package dependencies and version conflicts.
- **Update packages:** When updating packages, ensure that all related packages are updated to compatible versions.

## Development

### Building the Project

To build the project, run the following command from the root directory:

```sh
dotnet build src/MaxBot.sln
```

### Running Tests

To run the integration tests, run the following command from the root directory:

```sh
dotnet test test/Cli.Tests/Cli.Tests.csproj
```
