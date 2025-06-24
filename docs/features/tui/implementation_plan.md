# TUI Implementation Plan

## 1. Introduction

This document outlines the phased implementation plan for the MaxBot Terminal User Interface (TUI). The plan is structured to deliver functionality incrementally, focusing on building a stable foundation first and then layering on more complex features. Each phase includes the development of the required application code and the corresponding test cases defined in `test_case_specification.md`.

## 2. Development Philosophy

The implementation will follow a "requirements-first" black-box testing workflow, as mandated by the project's developer guidelines. For each feature, a skeleton test case will be created first, followed by the implementation, and finally the completion of the test to verify the functionality.

## 3. Implementation Phases

### Phase 1: Core Infrastructure and Basic Rendering

This phase focuses on setting up the foundational components of the TUI.

1.  **Setup `Spectre.Console`**: Integrate the `Spectre.Console` library into the `Cli` project.
2.  **Implement `ConsoleRenderer`**: Create the main `ConsoleRenderer` class. Implement the basic layout structure (e.g., a main panel for content and a footer for status).
3.  **Implement Event Bus**: Set up a simple, in-memory event bus for decoupling the `AppService` from the `ConsoleRenderer`.
4.  **Implement `ITuiCard` and `TextCard`**: Create the base `ITuiCard` interface and the first simple implementation, `TextCard`.
5.  **Test Case Development (TC-TUI-001)**: Create and complete the test case for rendering a simple text card.
6.  **Integrate with `AppService`**: Modify `AppService` to publish a `TextMessageReceivedEvent` and have the `ConsoleRenderer` subscribe to it and render a `TextCard`.

**Goal**: At the end of this phase, the application will have a basic TUI that can display simple text messages from the AI.

### Phase 2: Command Execution and Status Feedback

This phase builds on the core infrastructure to handle the primary workflow of executing commands.

1.  **Implement `CommandCard`**: Create the `CommandCard` component, capable of displaying a command, its status, and its output.
2.  **Implement Status Indicators**: Add logic to the `ConsoleRenderer` and `CommandCard` to display animated status indicators for API calls and running commands.
3.  **Refine `AppService` Events**: Implement the `CommandStatusChangedEvent` to provide granular updates (Started, OutputReceived, Completed, Failed).
4.  **Test Case Development (TC-TUI-004, TC-TUI-010, TC-TUI-011, TC-TUI-013)**: Create and complete the test cases for the `CommandCard`, status indicators, and interrupt handling.
5.  **Implement `MarkdownCard`**: Create the `MarkdownCard` and integrate the `Spectre.Console.Markdig` library.
6.  **Test Case Development (TC-TUI-002)**: Create and complete the test case for rendering markdown.

**Goal**: At the end of this phase, the TUI will be able to visualize the entire lifecycle of a command execution, including real-time feedback and markdown responses.

### Phase 3: File Operations and Interactive Prompts

This phase introduces more complex card types for file system interactions and user prompts.

1.  **Implement `FileCard` and `DiffCard`**: Create the components for displaying file contents (with syntax highlighting via `Spectre.Console.Extensions.Markup`) and color-coded diffs.
2.  **Implement `QuestionCard`**: Create the `QuestionCard` component, with support for both multiple-choice and Yes/No prompts.
3.  **Test Case Development (TC-TUI-005, TC-TUI-006, TC-TUI-007)**: Create and complete the test cases for the file and question cards.
4.  **Implement `TodoCard`**: Create the `TodoCard` for displaying checklists.
5.  **Test Case Development (TC-TUI-003)**: Create and complete the test case for the `TodoCard`.

**Goal**: At the end of this phase, the TUI will support the full range of content types required by the AI's toolset.

### Phase 4: Slash Commands and Final Polish

This final phase implements the slash command functionality and adds final usability touches.

1.  **Implement Slash Command UI**: Add the logic to the `ConsoleRenderer` to detect `/` input and display the command suggestion list.
2.  **Implement Filtering and Argument Placeholders**: Add the typeahead filtering and argument placeholder logic.
3.  **Test Case Development (TC-TUI-008, TC-TUI-009)**: Create and complete the test cases for the slash command functionality.
4.  **Implement Footer Status**: Implement the logic for displaying the current mode in the TUI footer.
5.  **Test Case Development (TC-TUI-012)**: Create and complete the test case for the footer.
6.  **Final Review**: Conduct a final review of the TUI against all non-functional requirements (Performance, Usability, Compatibility).

**Goal**: At the end of this phase, the TUI will be feature-complete and ready for user acceptance testing.
