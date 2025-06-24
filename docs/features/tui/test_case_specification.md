# TUI Test Case Specification

## 1. Introduction

This document defines the test cases for the MaxBot Terminal User Interface (TUI). The tests are derived from the requirements specified in `tool_requirements.md` and are designed to verify the functionality, usability, and robustness of the TUI implementation.

## 2. Assumptions and Implementation Notes

-   **Testing Framework**: Tests will be implemented using a standard .NET testing framework (e.g., xUnit).
-   **Mocking**: The `ChatClient` and `AppService` will be mocked to provide predictable data and simulate various scenarios without making live API calls.
-   **Console Simulation**: Tests will use `Spectre.Console.Testing.TestConsole` to capture and assert against the TUI's output without rendering to a live console. This allows for automated verification of the rendered output.
-   **Event-Driven Testing**: Tests will be structured to publish simulated UI events and verify that the `ConsoleRenderer` and its associated cards render the correct output in response.

## 3. Test Cases

The following table outlines the test cases, mapping each one to a specific requirement from `tool_requirements.md`.

| Test Case ID      | Requirement(s) Verified | Test Description                                                                                                   | Expected Result                                                                                             |
| ----------------- | ----------------------- | ------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| **Card Rendering**|                         |                                                                                                                    |                                                                                                             |
| TC-TUI-001        | FR-TUI-1                | Publish a `TextMessageReceivedEvent`.                                                                              | A `TextCard` is rendered with the correct message content.                                                  |
| TC-TUI-002        | FR-TUI-2                | Publish a `MarkdownMessageReceivedEvent` with markdown content.                                                    | A `MarkdownCard` is rendered, and the output shows correctly formatted markdown (e.g., headings, lists).  |
| TC-TUI-003        | FR-TUI-3                | Publish a `TodoPlanReceivedEvent` with a list of tasks.                                                            | A `TodoCard` is rendered, displaying the tasks with checkboxes.                                             |
| TC-TUI-004        | FR-TUI-4                | Publish a series of `CommandStatusChangedEvent`s (Running, Success, Error).                                        | A `CommandCard` is rendered and updates its status and output correctly for each event.                     |
| TC-TUI-005        | FR-TUI-5                | Publish a `QuestionPosedEvent` with multiple options.                                                              | A `QuestionCard` is rendered, displaying the question and the list of options.                              |
| TC-TUI-006        | FR-TUI-6, FR-TUI-12     | Publish a `FileReadEvent` with content.                                                                            | A `FileCard` is rendered, showing a preview of the content with syntax highlighting and an "expand" prompt. |
| TC-TUI-007        | FR-TUI-7                | Publish a `FilePatchedEvent` with diff data.                                                                       | A `DiffCard` is rendered, displaying a color-coded diff.                                                    |
| **Input/Interaction** |                     |                                                                                                                    |                                                                                                             |
| TC-TUI-008        | FR-TUI-9, FR-TUI-10     | Simulate the user typing `/` followed by characters.                                                               | The TUI displays a list of slash commands that filters correctly based on the input.                        |
| TC-TUI-009        | FR-TUI-11               | Simulate the user selecting a slash command that requires an argument.                                             | The input prompt displays the command followed by placeholder text for the argument.                        |
| **Status/Feedback** |                       |                                                                                                                    |                                                                                                             |
| TC-TUI-010        | FR-TUI-13               | Simulate the start of an API call.                                                                                 | The TUI displays an animated status indicator with a timer and token count.                                 |
| TC-TUI-011        | FR-TUI-14               | Simulate the start of a shell command.                                                                             | The TUI displays an animated "Running" indicator within a `CommandCard`.                                    |
| TC-TUI-012        | FR-TUI-15               | Set the application to a specific mode (e.g., "plan mode").                                                        | The TUI footer updates to display the correct mode indicator text.                                          |
| **Non-Functional**|                         |                                                                                                                    |                                                                                                             |
| TC-TUI-013        | NFR-TUI-3               | While a mocked command is "running," simulate a Ctrl+C interrupt.                                                  | The `CommandCard` updates its status to "Interrupted," and the application does not crash.                  |

This test suite provides comprehensive coverage of the TUI's requirements and will serve as the foundation for the verification and validation of the feature.
