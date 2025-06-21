<task name="Implement Next Priority Task">

<task_objective>
This workflow automates the implementation of the next priority task from the project plan, including creation of automation tests to verify Tool Operational Requirements (TORs), implementation of required features to make tests pass, and maintenance of requirement attribute upkeep. The workflow emphasizes iterative development with frequent validation and comprehensive documentation. The output includes updated test files, source code files, terminal output showing test results, and a summary report in markdown format.
</task_objective>

<detailed_sequence_steps>
# Implement Next Priority Task Process - Detailed Sequence of Steps

## 1. Identify Next Priority Task

1. Examine the project structure and current state.
   - Use the `list_files` tool to understand the current project structure.
   - Use the `list_code_definition_names` tool to get an overview of existing implementations.

2. Read the project plan to understand current status and priorities.
   - Use the `read_file` tool to examine `docs/project_plan.md`.
   - Analyze the content to identify tasks, their priorities, and current implementation status.

3. Cross-reference with existing specifications and trace matrix.
   - Use the `read_file` tool to examine `docs/specs/trace_matrix.md`.
   - Use the `search_files` tool to find related specifications and existing implementations.

4. Determine the next task to implement based on priority and dependencies.
   - Review task dependencies and prerequisites.
   - Identify the highest priority task that is ready for implementation.
   - Consider what can be built incrementally on existing code.

5. Ask the user to confirm the identified priority task.
   - Use the `ask_followup_question` tool to present the identified task with context.
   - Include brief rationale for why this task was selected.
   - Allow the user to confirm or specify a different task if needed.

## 2. Analyze Tool Operational Requirements (TORs)

1. Read relevant specification files to extract TORs for the identified task.
   - Use the `read_file` tool to examine specification files in `docs/specs/`.
   - Use the `search_files` tool with targeted regex patterns to find TOR references related to the task.
   - Look for both explicit TOR statements and implied requirements.

2. Document the specific TORs that need to be implemented.
   - Extract functional requirements, performance criteria, and acceptance criteria.
   - Identify any dependencies on other components or systems.
   - Create a clear mapping between requirements and testable outcomes.

3. Analyze existing codebase to understand current implementation state.
   - Use the `list_code_definition_names` tool to understand code structure in relevant directories.
   - Use the `search_files` tool to find existing related functionality and patterns.
   - Identify reusable components and architectural patterns to follow.

4. Validate TOR completeness and clarity.
   - Check if TORs are specific enough to create meaningful tests.
   - If TORs are ambiguous, use `ask_followup_question` to clarify with the user.
   - Document any assumptions made about unclear requirements.

## 3. Create Automation Tests

1. Check the trace matrix to ensure tests don't already exist.
   - Use the `read_file` tool to examine `docs/specs/trace_matrix.md`.
   - Verify that the identified TORs don't already have corresponding tests.
   - If tests exist, determine if they need enhancement or are sufficient.

2. Analyze existing test patterns and structure.
   - Use the `list_files` tool to examine the test directory structure.
   - Use the `read_file` tool to examine existing test files for patterns and conventions.
   - Identify the appropriate test framework and naming conventions.

3. Develop comprehensive tests that verify TOR compliance.
   - Start with failing tests that define the expected behavior (TDD approach).
   - Create or modify test files in the appropriate test directory.
   - Use the `write_to_file` or `replace_in_file` tools to implement tests.
   - Ensure tests cover all identified TORs and acceptance criteria.
   - Include both positive and negative test cases.

4. Validate tests compile and run (even if they fail).
   - Use the `execute_command` tool to compile and run the new tests.
   - Ensure test infrastructure is working before proceeding to implementation.
   - Fix any compilation or setup issues immediately.

5. Update the trace matrix to reflect new test coverage.
   - Use the `replace_in_file` tool to add new test entries to `docs/specs/trace_matrix.md`.
   - Link TORs to their corresponding test implementations with specific test method names.
   - Include test file paths and line numbers for precise traceability.

## 4. Implement Required Features

1. Analyze the failing tests to understand what functionality needs to be implemented.
   - Run the new tests using the `execute_command` tool to see current failures.
   - Document what features are missing or need modification.
   - Prioritize implementation order based on dependencies and complexity.

2. Plan the implementation approach.
   - Identify which existing files need modification vs. new files needed.
   - Consider the minimal viable implementation that satisfies the TORs.
   - Plan for incremental development with frequent validation points.

3. Implement features incrementally with frequent testing.
   - Start with the simplest implementation that makes one test pass.
   - Use the `write_to_file` or `replace_in_file` tools to implement features.
   - Follow existing code patterns and architectural conventions.
   - Run tests after each significant change using the `execute_command` tool.
   - Make incremental improvements based on test feedback.

4. Ensure code quality and maintainability.
   - Add appropriate error handling and input validation.
   - Include meaningful comments and documentation.
   - Follow the project's coding standards and conventions.
   - Consider edge cases and boundary conditions.

5. Validate implementation completeness.
   - Ensure all TOR requirements are satisfied by the implementation.
   - Verify that the implementation integrates properly with existing code.
   - Check that no existing functionality is broken.

## 5. Execute Verification

1. Run the complete test suite to ensure no regressions.
   - Use the `execute_command` tool to run all tests in the project.
   - Verify that both new and existing tests pass.
   - If any tests fail, analyze and fix issues before proceeding.

2. Validate that all TORs are satisfied by the implementation.
   - Review test results against the original TOR requirements.
   - Confirm that acceptance criteria are met.
   - Cross-reference with the trace matrix to ensure complete coverage.

3. Perform additional verification steps specific to the implemented feature.
   - Test edge cases and error conditions not covered by automated tests.
   - Verify integration with existing system components.
   - If applicable, perform manual testing or use `browser_action` for UI features.

4. Run any project-specific validation commands.
   - Execute build commands to ensure the project compiles cleanly.
   - Run any linting or code quality checks.
   - Execute acceptance tests if they exist (e.g., `test/local_exe_acceptance.ps1`).

5. Document any issues or limitations discovered during verification.
   - Note any TORs that may need refinement.
   - Identify potential improvements for future iterations.
   - Record any technical debt introduced.

## 6. Generate Summary Report

1. Organize outputs under the root directory `outputs/`.
   - Use the `list_files` tool to check if the `outputs/` directory exists.
   - Create the directory structure if needed using `write_to_file` with appropriate paths.
   - Create a timestamped subdirectory for this implementation session (format: `YYYY-MM-DD_HHMMSS`).

2. Collect and organize all relevant artifacts.
   - Capture final test execution output using `execute_command`.
   - Document all files that were created or modified during the implementation.
   - Gather any error messages or warnings that were resolved.

3. Create a comprehensive summary report with the following sections:
   - **Executive Summary**: High-level overview of what was accomplished
   - **Task Summary**: Description of the implemented priority task with context
   - **TOR Analysis**: List of TORs that were addressed with implementation details
   - **Test Implementation**: Details of tests created or modified with file paths and coverage
   - **Feature Implementation**: Summary of code changes and new functionality with file references
   - **Verification Results**: Test execution results and validation outcomes with metrics
   - **Trace Matrix Updates**: Changes made to requirement traceability with before/after comparison
   - **Technical Notes**: Any architectural decisions, patterns used, or technical debt
   - **Next Steps**: Recommendations for follow-up tasks or improvements with priorities

4. Create additional supporting documentation.
   - Generate a change log of all modified files.
   - Create a quick reference guide for the new functionality if applicable.
   - Document any new dependencies or configuration changes.

5. Use the `attempt_completion` tool to present the final results.
   - Include the path to the generated summary report.
   - Provide a concise overview of what was accomplished.
   - Include key metrics (tests added, files modified, TORs satisfied).
   - Reference any relevant terminal output or test results.
   - Suggest the next logical task to implement if apparent.

</detailed_sequence_steps>

</task>
