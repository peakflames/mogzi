<task name="Implement Next Priority Task">

<task_objective>
This workflow automates the implementation of the next priority task for the UI Refinement project plan which seeks to transform MaxBot's basic TUI into a sophisticated chat interface matching Gemini-CLI's look and feel. The plan solely focuses on implementation (no testing for now). The workflow emphasizes iterative development with frequent validation by the User at key points. The output includes source code files and the User validation of the changes meet expectations.
</task_objective>

<detailed_sequence_steps>
# Implement Next Priority Task Process - Detailed Sequence of Steps

## 1. Identify Next Priority Task

1. Examine the project structure and current state.
   - Use the `list_files` tool to understand the current project structure.
   

2. Read the project plan to understand current status and priorities.
   - Use the `read_file` tool to examine `docs/features/ui-refinement/simple-project-plan.md`.
   - Use the `read_file` tool to examine `docs/features/ui-refinement/maxbot-ui-refinement-plan.md`
   - Analyze the content to identify tasks, their priorities, and current implementation status.

3. Determine the next task to implement based on priority and dependencies.
   - Review task dependencies and prerequisites.
   - Identify the highest priority task that is ready for implementation.
   - Consider what can be built incrementally on existing code.

4. Ask the user to confirm the identified priority task.
   - Use the `ask_followup_question` tool to present the identified task with context.
   - Include brief rationale for why this task was selected.
   - Allow the user to confirm or specify a different task if needed.

## 2. Analyze Architecture and Design Intentions

1. **Gather Context from Developer Documentation.**
   - Use the `read_file` tool to examine `docs/features/ui-refinement/ui-arch-impl-comparison-of-gemini-cli-vs-max_bot.md`
   - Use the `read_file` tool to examine `docs/llmctx/Spectre.Console.md`
   - Create a clear mapping between requirements and testable outcomes.
  
2. Analyze existing codebase to understand current implementation state.
   - Use the `list_code_definition_names` tool to get an overview of existing implementations focusing solely on the current implementation `src/UI`.
   - Use the `search_files` tool to find existing related functionality and patterns on the current implementation `src/UI`.

3. **Check in with user.**
   - Summarize the findings from Architecture and Design Intentions analysis.
   - Use `ask_followup_question` to ensure the user is following along and to clarify any points before proceeding.

## 3. Propose Implementation Plan and Seek Approval

1. Formulate a detailed implementation plan based on the analysis.
   - Outline the files that will be created or modified.
   - Detail the feature implementation steps.
   - Detail what the User will be able to tangible see when manually running the with the new changes.
   - Optionally, as it is expensive, one can obtain addiitonal knowledge from the Gemini CLI UI codebase (typescript) at `tmp/gemini-cli/packages/cli/src`

2. Present the plan to the user for approval before implementing.
   - Use the `ask_followup_question` tool to present the summarized plan.
   - State clearly that implementation will not begin without explicit user approval.

3. Await user confirmation before proceeding.
   - Do not proceed to the next step until the user approves the proposed plan.

## 4. Implement Required Features

1.  **Engage user in an interactive implementation loop.**
    - For each main aspect of the feature:
        i. **Propose a small implementation step.**
           - Outline the specific change (e.g., "Next, I will add the `IAppService` dependency to the `AppComponent` constructor.").
           - Use `ask_followup_question` to get user approval before writing code.
        ii. **Implement the approved step.**
           - Use `write_to_file` or `replace_in_file` to make the code change.
        iii. **Run tests to validate the step.**
           - Use `execute_command` to run the relevant tests.
           - Share the result with the user.
        iv. **Repeat** until all tests pass and the feature is complete.

2.  Ensure code quality and maintainability throughout the process.
    - Add appropriate error handling and input validation.
    - Include meaningful comments and documentation.
    - Follow the project's coding standards and conventions.
    - Consider edge cases and boundary conditions.

3.  Validate final implementation completeness.
    - Ensure all TOR requirements are satisfied by the implementation.
    - Verify that the implementation integrates properly with existing code.
    - Check that no existing functionality is broken.

## 5. Perfrom Adhoc Verification

1. If the changes made can be visually as terminal output, then Work with the User to:
   1. Use the `execute_command` to run the MaxBot UI via `dotnet --project src/UI/UI.csproj`
   2. What the terminal command is running, ask the user perform an action that will trigger the code changes to display
   3. Ask the user to exit the running application and you will read the terminal output to analyze the output match the expectation
   4. Ask the user if the UI changes are meeting the expectations for this incremental change


## 6. Increment Wrap Up

1. Update project documentation with next priority task.
   - Use the `replace_in_file` tool to update `docs/features/ui-refinement/simple-project-plan.md`.
   - Mark the completed task as "✅ **COMPLETED**" in the Phase sections.
   - Update the "Short-term Goals" section to clearly mark the "**NEXT PRIORITY**" task.
   - Ensure the next recommended task is clearly documented for future sessions.


2. Use the `attempt_completion` tool to declare completion. Keep it short, no more than 50 words

</detailed_sequence_steps>

</task>
