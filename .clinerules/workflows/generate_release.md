<task name="Generate Release">

<task_objective>
This workflow automates the process of preparing a new release. It checks the project version, runs all tests, and executes acceptance tests to ensure the release is stable.
</task_objective>

<detailed_sequence_steps>
# Generate Release Process - Detailed Sequence of Steps

## 1. Verify Project Version

1.  Read the contents of the `src/Mogzi.TUI/Mogzi.TUI.csproj` file to find the current version number.
    - Use the `read_file` tool to get the file content.
    - Extract the version from the `<Version>` tag.

2.  Ask the user to confirm if the current version is correct for the new release.
    - Use the `ask_followup_question` tool with the current version.
    - Provide options for the user to confirm or provide a new version number.

3.  If the user provides a new version number, update the `src/Mogzi.TUI/Mogzi.TUI.csproj` file.
    - Use the `replace_in_file` tool to replace the old version with the new one.

## 2. Run Unit and Integration Tests

1.  Check the test results for any failures.
    - If failures occur, do not attempt to solve them.
    - Report the issue to the user, including the error output and a brief analysis of the potential cause.
    - Stop the workflow.

## 3. Run Acceptance Tests

1.  Execute the acceptance test script.
    - Use the `execute_command` tool with the command `powershell -File test/local_exe_acceptance.ps1`.

2.  Check the script output for any failures.
    - If failures occur, do not attempt to solve them.
    - Report the issue to the user, including the error output and a brief analysis of the potential cause.
    - Stop the workflow.

## 4. Complete Release Preparation

1.  If all tests pass, inform the user that the release is ready.
    - Use the `attempt_completion` tool to present the final result.
    - The result should state that the version has been confirmed and all tests have passed, and the project is ready for release.

</detailed_sequence_steps>

</task>
