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

## Phase 4: CLI Commands and Display Enhancements (COMPLETED)

**Status**: ✅ **COMPLETED** - January 7, 2025

This phase focused on implementing comprehensive CLI commands for session management and improving the display formatting for better user experience.

1.  **CLI Session Commands Implementation**:
    -   ✅ **Implemented `SessionCommand` class** in `src/Mogzi.TUI/Commands/SessionCommand.cs`
        - Provides `mogzi session list` command for listing available sessions
        - Provides `mogzi session info <name_or_id>` command for detailed session information
        - Enhanced session lookup supporting name, full GUID, and partial GUID matching
        - Comprehensive error handling and help text
    
2.  **Session List Limit Integration**:
    -   ✅ **Integrated `sessionListLimit` configuration** from `ApplicationConfiguration`
        - Both CLI and TUI session listing now respect the configured limit (default: 10)
        - Consistent behavior across all session listing interfaces
        - Updated `SessionListProvider` to use the same limiting logic
    
3.  **Table Display Enhancements**:
    -   ✅ **Improved table column widths** for better readability:
        - Name: 25 characters (allows multi-word session names)
        - ID: 15 characters (shows truncated IDs with "..." prefix)
        - Created: 18 characters (full date and time display)
        - Last Modified: 18 characters (full date and time display)
        - Initial Prompt: 50 characters (prevents truncation of longer prompts)
    
4.  **Contextual Header Updates**:
    -   ✅ **Updated CLI command table header** to show `"Available Chat Sessions (last X recently used)"`
    -   ✅ **Enhanced TUI slash command display** in `UserSelectionPanel` to show contextual headers
        - Detects when SessionListProvider is active
        - Shows session count and "recently used" context
        - Maintains backward compatibility with other selection providers
    
5.  **Testing and Validation**:
    -   ✅ **All acceptance tests passing** for CLI session commands
    -   ✅ **Verified slash command functionality** with updated display headers
    -   ✅ **Session list limit functionality** working correctly across both interfaces
    -   ✅ **Table formatting improvements** validated through test output

**Key Files Modified**:
- `src/Mogzi.TUI/Commands/SessionCommand.cs` - New CLI command implementation
- `src/Mogzi.TUI/Components/UserSelectionPanel.cs` - Enhanced with contextual headers
- `src/Mogzi.TUI/Services/SessionListProvider.cs` - Cleaned up unnecessary methods
- `README.md` - Updated with correct CLI command examples

**Technical Achievements**:
- **Flexible Session Lookup**: Supports exact name matching, full GUID, and partial GUID matching
- **Consistent UX**: Both CLI and TUI interfaces show the same session limit context
- **Improved Readability**: Wider table columns prevent text truncation
- **Robust Error Handling**: Comprehensive error messages and help text
- **Architecture Separation**: CLI commands independent of TUI infrastructure while maintaining consistent behavior

This phase successfully completed the user-facing session management commands and significantly improved the display quality and user experience for session listing across all interfaces.
