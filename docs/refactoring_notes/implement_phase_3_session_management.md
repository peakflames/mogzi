# Session Management Implementation Plan

This document outlines the implementation plan for the session management feature, following the requirements and design specified in the project documentation.

## Phase 1: Core Implementation & Prototyping

The goal of this phase is to build the foundational pieces of the session management system. This allows for rapid prototyping of the core data structures and service interactions before formal testing begins.

1.  **Create `Session` Domain Entity**:
    -   Create the `Session` class in `src/Mogzi.Core/Domain/Session.cs`.
    -   The class will include properties for `Id`, `Name`, `CreatedAt`, `LastModifiedAt`, `History`, and `InitialPrompt` as defined in the design document.

2.  **Scaffold `SessionManager` Service**:
    -   Create the `SessionManager` class in `src/Mogzi.TUI/Services/SessionManager.cs`.
    -   Implement the class structure with placeholder methods for all public functions (`CreateNewSessionAsync`, `LoadSessionAsync`, `SaveCurrentSessionAsync`, `ListSessionsAsync`, etc.).

3.  **Integrate into DI and Context**:
    -   Register `SessionManager` as a singleton service in `src/Mogzi.TUI/Infrastructure/ServiceConfiguration.cs`.
    -   Update the `ITuiContext` interface to include a reference to the `SessionManager`.

4.  **Prototype Core Logic**:
    -   Implement the essential logic for `CreateNewSessionAsync` to establish a new session object.
    -   Implement the core logic for `SaveCurrentSessionAsync`, including the atomic file-writing mechanism (write to temp file, then rename) to prevent data corruption.

## Phase 2: Test-Driven Development (TDD) of Features

With the core components in place, we will use a strict TDD approach to build out the features. For each feature, a failing black-box acceptance test will be written first, followed by the implementation code to make the test pass.

1.  **TDD for New Session Creation**:
    -   **Test**: Write an acceptance test to verify that a new session file is created in the `~/.mogzi/chats/` directory when the application starts without a `--session` argument.
    -   **Implement**: Hook the `SessionManager.CreateNewSessionAsync` method into the application's startup sequence in `FlexColumnTuiApp.cs`.

2.  **TDD for Continuous Persistence**:
    -   **Test**: Write an acceptance test that sends a message and then verifies that the corresponding session file on disk is immediately updated with the new history.
    -   **Implement**: Modify `HistoryManager` to call `SessionManager.SaveCurrentSessionAsync()` whenever a message is added to the history.

3.  **TDD for Loading Existing Sessions**:
    -   **Test**: Write an acceptance test that starts the application with a `--session <session_id>` argument and verifies that the correct chat history is loaded and displayed.
    -   **Implement**: Add the command-line argument parsing logic in `Program.cs` and fully implement the `SessionManager.LoadSessionAsync` method.

4.  **TDD for Listing Sessions**:
    -   **Test**: Write an acceptance test that creates several mock session files, runs the `/session list` command, and asserts that the output correctly formats and displays the metadata for each session.
    -   **Implement**: Build out the `SessionManager.ListSessionsAsync` method and integrate it with the `SlashCommandProcessor`.

5.  **TDD for Handling Corrupted Sessions**:
    -   **Test**: Write an acceptance test that attempts to load a deliberately corrupted session file and asserts that the file is renamed to `.corrupted` and a new, empty session is started.
    -   **Implement**: Add the error handling and recovery logic to `SessionManager.LoadSessionAsync`.

## Phase 3: Finalizing Lower-Priority Features

Once the critical functionality is implemented and robustly tested, the remaining lower-priority features will be addressed.

1.  **Implement `/session clear`**:
    -   Write an acceptance test for the clear command.
    -   Implement the `SessionManager.ClearCurrentSessionAsync` method and the corresponding slash command.

2.  **Implement `/session rename <name>`**:
    -   Write an acceptance test for the rename command.
    -   Implement the `SessionManager.RenameSessionAsync` method and the corresponding slash command.

3.  **Refinement**:
    -   Refine any UI elements related to session management based on the implemented functionality.
    -   Conduct a final review of the code for this feature, checking for clarity, documentation, and adherence to coding standards.
