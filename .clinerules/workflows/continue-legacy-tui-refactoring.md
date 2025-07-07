<task name="Continue Legacy TUI Refactoring">

<task_objective>
This workflow prepares the AI model's context with all relevant documentation, plans, source code, and test files to understand the ongoing TUI refactoring effort. It will then propose the next implementation task from the Phase 3 plan to the user and, upon approval, begin working on it.
</task_objective>

<detailed_sequence_steps>
# Continue Legacy TUI Refactoring Process - Detailed Sequence of Steps

## 1. Context Preparation: Load Documentation and Plans

1.  Use `read_file` to read `docs/process/03_architecture.md` to understand the target architecture.
2.  Use `read_file` to read `docs/process/04_00_design.md` to understand the high-level design specifications.
3.  Use `read_file` to read `docs/refactoring_notes/README.md` to understand the overall refactoring strategy.
4.  Use `read_file` to read `docs/refactoring_notes/implement_phase_1.md` to understand the completed foundational work.
5.  Use `read_file` to read `docs/refactoring_notes/implement_phase_2.md` to understand the component extraction and the lessons learned.
6.  Use `read_file` to read `docs/refactoring_notes/implement_phase_3.md` to understand the remaining work and priorities.

## 2. Context Preparation: Load Key Source Code

1.  Use `read_file` to read `src/Mogzi.TUI/App/FlexColumnTuiApp.cs` to understand the current, refactored application entry point.
2.  Use `read_file` to read `src/Mogzi.TUI/App/LegacyFlexColumnTuiApp.cs` as a reference for required features and legacy implementation details.
3.  Use `read_file` to read `src/Mogzi.PawPrints/ScrollbackTerminal.cs` as it is a key component in the rendering issues.
4.  Use `read_file` to read `src/Mogzi.TUI/State/TuiStateManager.cs` to understand the core state machine.
5.  Use `read_file` to read `src/Mogzi.TUI/Components/TuiComponentManager.cs` to understand component lifecycle and rendering.
6.  Use `read_file` to read `src/Mogzi.TUI/Components/FlexColumnMediator.cs` to understand how components and services communicate.

## 3. Context Preparation: Load Key Test Files

1.  Use `read_file` to read `test/Mogzi.TUI.Tests/ChatWorkflowAcceptanceTests.cs` to understand the existing acceptance tests for chat workflows.
2.  Use `read_file` to read `test/Mogzi.TUI.Tests/NullReferenceExceptionTest.cs` to understand how specific regression tests are structured.
3.  Use `read_file` to read `test/Mogzi.TUI.Tests/EnterKeyDebugTest.cs` to understand how specific input handling tests are structured.

## 4. Propose Next Implementation Task

1.  Analyze all the loaded context to identify the highest priority task from the "UI/UX Stabilization" section of the Phase 3 plan.
2.  Formulate a clear, concise proposal for the next implementation step. The proposal should state the problem to be solved and the proposed approach.
3.  Use the `ask_followup_question` tool to present the proposal to the user and get their approval to proceed.

## 5. Begin Implementation

1.  Once the user approves the proposal, begin working on the implementation.
2.  This will involve a sequence of `read_file`, `replace_in_file`, and `execute_command` (for running tests) tool uses.
3.  Follow the Test-Driven Development (TDD) approach described in the Phase 2 "Lessons Learned":
    i.  First, create a new acceptance test that fails because of the bug.
    ii. Then, implement the code changes to fix the bug and make the test pass.
4.  Use the `attempt_completion` tool to present the final result once the task is complete and all tests are passing.

</detailed_sequence_steps>

</task>
