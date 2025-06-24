# TUI Tool Requirements

## 1. Introduction

This document specifies the functional and non-functional requirements for the MaxBot Terminal User Interface (TUI). It builds upon the concepts outlined in the `concept_of_operations.md` and defines the specific capabilities required to deliver the intended user experience.

## 2. Functional Requirements

### 2.1. TUI Card Rendering

The TUI must support the rendering of various "cards," each tailored to a specific type of content.

-   **FR-TUI-1 (Text Card)**: The TUI shall render simple text content, used for displaying AI chat messages and informational output.
-   **FR-TUI-2 (Markdown Card)**: The TUI shall render markdown-formatted text, supporting basic markdown syntax (headings, lists, bold, italics).
-   **FR-TUI-3 (Todo Card)**: The TUI shall render a checklist of tasks, with checkboxes to indicate the status of each item.
-   **FR-TUI-4 (Command Card)**: The TUI shall display a shell command, its real-time status (e.g., "Running," "Success," "Error," "Interrupted"), and its output.
-   **FR-TUI-5 (Question Card)**: The TUI shall display a question to the user and provide a list of interactive options for their response. This must support both multiple-choice and simple Yes/No formats.
-   **FR-TUI-6 (File Content Card)**: The TUI shall display the contents of a file, with support for syntax highlighting and an option to expand or collapse long content.
-   **FR-TUI-7 (Diff Card)**: The TUI shall display a color-coded, line-by-line diff of file changes (additions and deletions).

### 2.2. Input and Interaction

-   **FR-TUI-8 (Persistent Input)**: A text input box shall always be visible at the bottom of the TUI for user entry.
-   **FR-TUI-9 (Slash Commands)**: The TUI shall detect when the user types `/` and display a list of available slash commands.
-   **FR-TUI-10 (Command Filtering)**: The list of slash commands shall filter in real-time as the user types.
-   **FR-TUI-11 (Command Arguments)**: The TUI shall display placeholder text (e.g., `[argument]`) to indicate that a selected slash command requires an argument.
-   **FR-TUI-12 (Content Expansion)**: The user shall be able to expand and collapse content that exceeds a certain length (e.g., file contents, command output).

### 2.3. Status and Feedback

-   **FR-TUI-13 (API Status Indicator)**: The TUI shall display an animated indicator, token count, and elapsed time when an API call to the LLM is in progress.
-   **FR-TUI-14 (Command Status Indicator)**: The TUI shall display an animated indicator when a shell command is running.
-   **FR-TUI-15 (Mode Indicator)**: The TUI footer shall display the current operational mode (e.g., "plan mode on," "auto-accept edits on").

## 3. Tool-to-Card Mapping

The following table defines how the output of each AI tool will be mapped to a specific TUI card for rendering.

| Tool                    | TUI Card Type       | Key Information Displayed                               |
| ----------------------- | ------------------- | ------------------------------------------------------- |
| `execute_command`       | `CommandCard`       | Command, status, stdout/stderr                          |
| `write_to_file`         | `FileContentCard`   | File path, content preview, line count                  |
| `replace_in_file`       | `DiffCard`          | File path, color-coded diff view                        |
| `list_files`            | `FileContentCard`   | Directory path, expandable file tree                    |
| `read_file`             | `FileContentCard`   | File path, content preview, line count                  |
| `ask_followup_question` | `QuestionCard`      | The question, list of selectable options                |
| `attempt_completion`    | `TextCard`          | Final completion message                                |

## 4. Non-Functional Requirements

-   **NFR-TUI-1 (Performance)**: The TUI must remain responsive and performant, even when displaying large amounts of data or handling rapid updates.
-   **NFR-TUI-2 (Usability)**: The TUI must be intuitive and easy to use, with clear visual cues and consistent interaction patterns.
-   **NFR-TUI-3 (Robustness)**: The TUI must handle user interruptions (e.g., Ctrl+C) gracefully, terminating the appropriate process without crashing the application.
-   **NFR-TUI-4 (Compatibility)**: The TUI must render correctly across different terminal emulators on Windows, macOS, and Linux.
