<task name="TUI Implement Next Priority Task">

<task_objective>
This workflow automates the implementation of the next priority task for the Terminal User Interface (TUI) feature, following the phased implementation plan defined in the TUI documentation. The workflow emphasizes requirements-first black-box testing, event-driven architecture implementation, and comprehensive validation using Spectre.Console testing framework. The output includes updated test files, TUI component implementations, event system code, terminal output showing test results, project tracker updates, and a summary report in markdown format.
</task_objective>

<detailed_sequence_steps>
# TUI Implement Next Priority Task Process - Detailed Sequence of Steps

## 1. Identify Next TUI Priority Task

1. Examine the current TUI implementation state and project structure.
   - Use the `list_files` tool to understand the current project structure, focusing on `src/Cli/` and TUI-related directories.
   - Use the `list_code_definition_names` tool to get an overview of existing TUI implementations.

2. Read the TUI project tracker to understand current status and priorities.
   - Use the `read_file` tool to examine `docs/features/tui/project_tracker.md`.
   - Analyze the content to identify completed tasks, current phase, and next priority items.

3. Cross-reference with TUI implementation plan and requirements.
   - Use the `read_file` tool to examine `docs/features/tui/implementation_plan.md`.
   - Use the `read_file` tool to examine `docs/features/tui/tool_requirements.md`.
   - Use the `search_files` tool to find related TUI specifications and existing implementations.

4. Determine the next TUI task to implement based on phase priority and dependencies.
   - Review the 4-phase implementation plan (Core Infrastructure → Command Execution → File Operations → Slash Commands).
   - Identify the highest priority task within the current phase that is ready for implementation.
   - Consider dependencies on Spectre.Console integration and event system components.

5. Ask the user to confirm the identified TUI priority task.
   - Use the `ask_followup_question` tool to present the identified task with TUI context.
   - Include brief rationale for why this task was selected within the TUI roadmap.
   - Allow the user to confirm or specify a different TUI task if needed.

## 2. Analyze TUI Requirements and Test Cases

1. Read relevant TUI specification files to extract requirements for the identified task.
   - Use the `read_file` tool to examine `docs/features/tui/tool_requirements.md`.
   - Use the `read_file` tool to examine `docs/features/tui/test_case_specification.md`.
   - Use the `search_files` tool with targeted regex patterns to find specific functional requirements (FR-TUI-*) related to the task.

2. Document the specific TUI requirements that need to be implemented.
   - Extract functional requirements for card rendering, input/interaction, or status feedback.
   - Identify tool-to-card mapping requirements from the specification.
   - Create a clear mapping between TUI requirements and testable outcomes using Spectre.Console.Testing.

3. Analyze existing TUI codebase to understand current implementation state.
   - Use the `list_code_definition_names` tool to understand code structure in `src/Cli/UI/` and related directories.
   - Use the `search_files` tool to find existing TUI functionality, event handling patterns, and Spectre.Console usage.
   - Identify reusable TUI components and architectural patterns to follow.

4. Validate TUI requirement completeness and clarity.
   - Check if requirements are specific enough to create meaningful Spectre.Console tests.
   - If requirements are ambiguous, use `ask_followup_question` to clarify with the user.
   - Document any assumptions made about TUI behavior or event handling.

## 3. Propose TUI Implementation Plan and Seek Approval

1. Formulate a detailed TUI implementation plan based on the analysis.
   - Outline the TUI files that will be created or modified (ConsoleRenderer, ITuiCard implementations, event classes).
   - Describe the Spectre.Console tests that will be implemented to verify TUI requirements.
   - Detail the event-driven architecture implementation steps and card rendering logic.

2. Present the TUI plan to the user for approval before implementing.
   - Use the `ask_followup_question` tool to present the summarized TUI implementation plan.
   - State clearly that TUI implementation will not begin without explicit user approval.

3. Await user confirmation before proceeding.
   - Do not proceed to the next step until the user approves the proposed TUI plan.

## 4. Create TUI Automation Tests

1. Check the TUI test case specification to ensure tests align with requirements.
   - Use the `read_file` tool to examine `docs/features/tui/test_case_specification.md`.
   - Verify that the identified requirements have corresponding test cases defined (TC-TUI-*).
   - If tests exist, determine if they need enhancement or are sufficient for the current task.

2. Analyze existing TUI test patterns and Spectre.Console.Testing structure.
   - Use the `list_files` tool to examine the test directory structure for TUI-related tests.
   - Use the `read_file` tool to examine existing test files for Spectre.Console.Testing patterns and conventions.
   - Identify the appropriate test framework setup for `TestConsole` and event simulation.

3. Develop comprehensive TUI tests that verify requirement compliance.
   - Start with failing tests that define the expected TUI behavior (TDD approach).
   - Create or modify test files in the appropriate test directory using Spectre.Console.Testing.
   - Use the `write_to_file` or `replace_in_file` tools to implement TUI tests.
   - Ensure tests cover card rendering, event handling, and user interaction scenarios.
   - Include both positive and negative test cases for TUI components.

4. Validate TUI tests compile and run (even if they fail).
   - Use the `execute_command` tool to compile and run the new TUI tests.
   - Ensure Spectre.Console.Testing infrastructure is working before proceeding to implementation.
   - Fix any compilation or setup issues immediately.

5. Update the TUI project tracker to reflect new test coverage.
   - Use the `replace_in_file` tool to add new test entries to `docs/features/tui/project_tracker.md`.
   - Link TUI requirements to their corresponding test implementations with specific test method names.
   - Include test file paths and line numbers for precise traceability.

## 5. Implement Required TUI Features

1. Analyze the failing TUI tests to understand what functionality needs to be implemented.
   - Run the new TUI tests using the `execute_command` tool to see current failures.
   - Document what TUI features are missing or need modification (cards, events, rendering logic).
   - Prioritize implementation order based on TUI architecture dependencies and complexity.

2. Plan the TUI implementation approach.
   - Identify which existing TUI files need modification vs. new TUI components needed.
   - Consider the minimal viable TUI implementation that satisfies the requirements.
   - Plan for incremental development with frequent validation points using Spectre.Console.

3. Implement TUI features incrementally with frequent testing.
   - Start with the simplest TUI implementation that makes one test pass.
   - Use the `write_to_file` or `replace_in_file` tools to implement TUI features.
   - Follow existing TUI code patterns and event-driven architectural conventions.
   - Run TUI tests after each significant change using the `execute_command` tool.
   - Make incremental improvements based on Spectre.Console test feedback.

4. Ensure TUI code quality and maintainability.
   - Add appropriate error handling and input validation for TUI components.
   - Include meaningful comments and documentation for event handling and card rendering.
   - Follow the project's coding standards and TUI architectural conventions.
   - Consider edge cases and boundary conditions for TUI interactions.

5. Validate TUI implementation completeness.
   - Ensure all TUI requirements are satisfied by the implementation.
   - Verify that the TUI implementation integrates properly with existing ConsoleRenderer and event system.
   - Check that no existing CLI functionality is broken by TUI changes.

## 6. Execute TUI Verification

1. Run the complete TUI test suite to ensure no regressions.
   - Use the `execute_command` tool to run all TUI tests in the project.
   - Verify that both new and existing TUI tests pass.
   - If any TUI tests fail, analyze and fix issues before proceeding.

2. Validate that all TUI requirements are satisfied by the implementation.
   - Review TUI test results against the original functional requirements (FR-TUI-*).
   - Confirm that TUI acceptance criteria are met for card rendering and event handling.
   - Cross-reference with the TUI test case specification to ensure complete coverage.

3. Perform additional TUI verification steps specific to the implemented feature.
   - Test TUI edge cases and error conditions not covered by automated tests.
   - Verify TUI integration with existing CLI system components.
   - If applicable, perform manual testing of TUI rendering and interaction behavior.

4. Run any project-specific validation commands.
   - Execute build commands to ensure the project compiles cleanly with TUI changes.
   - Run any linting or code quality checks on TUI components.
   - Execute acceptance tests if they exist (e.g., `test/local_exe_acceptance.ps1`).

5. Document any TUI issues or limitations discovered during verification.
   - Note any TUI requirements that may need refinement.
   - Identify potential TUI improvements for future iterations.
   - Record any technical debt introduced in TUI implementation.

## 7. Generate TUI Summary Report

1. Organize TUI outputs under the root directory `outputs/`.
   - Use the `list_files` tool to check if the `outputs/` directory exists.
   - Create the directory structure if needed using `write_to_file` with appropriate paths.
   - Create a timestamped subdirectory for this TUI implementation session (format: `tui_YYYY-MM-DD_HHMMSS`).

2. Collect and organize all relevant TUI artifacts.
   - Capture final TUI test execution output using `execute_command`.
   - Document all TUI files that were created or modified during the implementation.
   - Gather any TUI-specific error messages or warnings that were resolved.

3. Create a comprehensive TUI summary report with the following sections:
   - **Executive Summary**: High-level overview of TUI functionality accomplished
   - **TUI Task Summary**: Description of the implemented TUI priority task with context
   - **TUI Requirement Analysis**: List of TUI requirements (FR-TUI-*) that were addressed with implementation details
   - **TUI Test Implementation**: Details of Spectre.Console tests created or modified with file paths and coverage
   - **TUI Feature Implementation**: Summary of TUI code changes and new functionality with file references
   - **TUI Verification Results**: Test execution results and validation outcomes with metrics
   - **TUI Project Tracker Updates**: Changes made to TUI requirement traceability with before/after comparison
   - **TUI Technical Notes**: Any architectural decisions, event patterns used, or technical debt
   - **TUI Next Steps**: Recommendations for follow-up TUI tasks or improvements with phase priorities

4. Create additional TUI supporting documentation.
   - Generate a change log of all TUI-modified files.
   - Create a quick reference guide for the new TUI functionality if applicable.
   - Document any new TUI dependencies or Spectre.Console configuration changes.

5. Update TUI project documentation with next priority task.
   - Use the `replace_in_file` tool to update `docs/features/tui/project_tracker.md`.
   - Mark the completed TUI task as "✅ **COMPLETED**" in the appropriate Phase sections.
   - Update the "Current Phase Status" section to clearly mark the "**NEXT PRIORITY**" TUI task.
   - Update TUI implementation and verification status percentages.
   - Ensure the next recommended TUI task is clearly documented for future sessions.

6. Update System Documentation and Change Log.
   - Analyze if the new TUI functionality requires any changes to system documentation.
   - Append a user-facing TUI update to `CHANGELOG.md` under the `[TBD]` or `[Unreleased]` section.
   - Update `README.md` if the TUI changes affect user-facing functionality or installation instructions.

7. Use the `attempt_completion` tool to present the final TUI results.
   - Include the path to the generated TUI summary report.
   - Provide a concise overview of what TUI functionality was accomplished.
   - Include key TUI metrics (tests added, files modified, requirements satisfied).
   - Reference any relevant terminal output or TUI test results.
   - Clearly state where the next TUI priority task is documented for future reference.

</detailed_sequence_steps>

</task>
