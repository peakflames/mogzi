# TUI Concept of Operations

## 1. Introduction

This document defines the concept of operations for the new Terminal User Interface (TUI) for MaxBot. It describes the system from the user's perspective, outlining the high-level vision, user journey, and core operational principles.

## 2. High-Level Concept

The MaxBot TUI will be a rich, interactive, and dynamic interface that provides a clear and intuitive user experience. The core concept is a multi-region display where the conversation and the AI's operations are rendered as a series of distinct, interactive "cards."

This card-based metaphor allows for a clean separation of concerns, where different types of information (e.g., chat messages, file diffs, command outputs) are presented in a structured and contextually appropriate manner.

## 3. User Journey

The following sequence outlines a typical user journey, illustrating the TUI's behavior at each step:

1.  **Startup**: The user launches the application and is greeted with a welcome banner and some helpful tips. An input prompt is displayed at the bottom of the screen, ready for user input.
2.  **User Input**: The user types a request into the input prompt. If the user types a `/`, a typeahead suggestion box appears, showing a list of available slash commands.
3.  **Command Execution**: The user submits their request. The TUI displays the user's message and then shows a status indicator (e.g., a spinning star) to signify that an API request is in progress.
4.  **AI Response & Tool Calls**: The AI processes the request. The interaction unfolds as a series of cards:
    *   A **Text Card** appears with the AI's initial text response.
    *   A **Todo Card** might be displayed, outlining the steps the AI will take.
    *   An **Execute Command Card** appears, showing the shell command being run and its real-time status.
    *   If the command requires user confirmation, a **Question Card** is rendered with interactive options (e.g., Yes/No).
    *   If a file is written, a **File Content Card** is shown with a preview of the new content.
    *   If a file is modified, a **Diff Card** is displayed with a color-coded view of the changes.
5.  **Completion**: The process continues until the task is complete. The input prompt remains available at the bottom of the screen throughout the entire interaction.

## 4. Core Principles

The TUI design is guided by the following principles:

-   **Clarity**: The UI will always maintain a clear distinction between user input, AI-generated text, and the outputs of tool calls. Consistent styling and iconography will be used to make the source of all information immediately apparent.
-   **Interactivity**: The user is not a passive observer. The TUI will provide interactive elements, allowing the user to answer questions, expand collapsed content, and guide the AI's workflow.
-   **Real-time Feedback**: The user will be kept informed of the system's status at all times. Long-running operations, such as API calls and shell commands, will be accompanied by status indicators, timers, and token counts, providing transparency and reducing uncertainty.
-   **Graceful Error Handling**: When errors occur (e.g., a command fails), they will be displayed in a distinct, clearly formatted error card, allowing the user to quickly understand what went wrong.
