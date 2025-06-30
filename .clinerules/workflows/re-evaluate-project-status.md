<task name="Re-evaluate Project Status">

<task_objective>
This workflow systematically re-evaluates the actual implementation progress of a requirements-driven project against documented status in traceability matrices and project plans. It identifies discrepancies between actual implementation and documented status, updates all related documentation to reflect current reality, and provides comprehensive analysis of project completion. The output includes updated traceability matrices, project plans, system requirements, and a detailed completion analysis report.
</task_objective>

<detailed_sequence_steps>
# Re-evaluate Project Status Process - Detailed Sequence of Steps

## 1. Assess Current Documentation State

1. Examine the project structure and identify key documentation artifacts.
   - Use the `list_files` tool to understand the current project structure.
   - Identify documentation directories (typically `docs/`)
   - Locate key files: project plans, traceability matrices, system requirements, references, and README files.

2. Read existing project documentation to understand documented status.
   - Use the `read_file` tool to examine `docs/project_plan.md` for current implementation status claims.
   - Use the `read_file` tool to examine `docs/process/trace_matrix.md` for requirement verification status.
   - Note documented completion percentages, phase status, and verification claims.

3. Identify potential sources of actual implementation evidence.
   - Use the `read_file` tool to examine `CHANGELOG.md` for recent implementation work.
   - Use the `read_file` tool to examine `README.md` for current capabilities and features.
   - Identify source code directories and test directories for implementation evidence.

4. Ask the user to confirm the scope and focus of the re-evaluation.
   - Use the `ask_followup_question` tool to confirm which phase or requirements to focus on.
   - Ask if there are specific areas where they suspect documentation is out of date.
   - Confirm the expected level of detail for the analysis.

## 2. Analyze Actual Implementation Evidence

1. Examine the source code to identify implemented functionality.
   - Use the `list_code_definition_names` tool to get an overview of implemented components.
   - Use the `read_file` tool to examine key implementation files identified in documentation.
   - Use the `search_files` tool to find specific functionality mentioned in requirements.
   - Document what tools, features, and capabilities are actually implemented.

2. Analyze test coverage to verify implementation claims.
   - Use the `list_files` tool to examine test directory structure.
   - Use the `read_file` tool to examine test files to understand what functionality is verified.
   - Use the `search_files` tool to find test cases related to specific requirements.
   - Identify comprehensive test suites, integration tests, and acceptance tests.

3. Examine acceptance tests and integration evidence.
   - Use the `read_file` tool to examine acceptance test scripts (e.g., `test/local_exe_acceptance.ps1`).
   - Look for end-to-end testing that demonstrates working functionality.
   - Identify any automated verification of system integration.

4. Document findings systematically.
   - Create a mapping between requirements and actual implementation evidence.
   - Note discrepancies between documented status and actual implementation.
   - Identify requirements that are implemented but not documented as such.
   - Identify requirements that are documented as implemented but lack evidence.

## 3. Update Traceability Matrix

1. Analyze each requirement against implementation evidence.
   - For each requirement in the traceability matrix, determine actual implementation status.
   - Classify status as: Implemented, Partially Implemented, or Not Implemented.
   - Classify verification status as: Verified, Partially Verified, Demonstrable, or Not Verified.

2. Update implementation components with actual evidence.
   - Use the `replace_in_file` tool to update implementation component references.
   - Include specific file names, class names, and method names where applicable.
   - Add newly discovered implementation components not previously documented.

3. Update test case references with actual test evidence.
   - Use the `replace_in_file` tool to update test case references with actual test file paths.
   - Include specific test method names and test file locations.
   - Add newly discovered test coverage not previously documented.

4. Update verification status based on evidence.
   - Use the `replace_in_file` tool to update verification status for each requirement.
   - Change status from "Not Verified" to "Verified" where comprehensive tests exist.
   - Update status to "Partially Verified" where some but not complete verification exists.
   - Add evidence references for verification claims.

5. Update summary statistics and phase completion status.
   - Recalculate overall verification percentages based on updated individual statuses.
   - Update phase completion status (e.g., mark Phase 1 as complete if all critical requirements are verified).
   - Use the `replace_in_file` tool to update summary sections with accurate statistics.

## 4. Update Project Plan

1. Update implementation status based on actual evidence.
   - Use the `replace_in_file` tool to update overall implementation percentages.
   - Update the "Current State" section with accurate completion statistics.
   - Mark completed phases explicitly (e.g., "✅ **Phase 1 Complete**").

2. Update tool implementation status table.
   - Review the tool implementation status table against actual code evidence.
   - Change status from "❌ Not Implemented" to "✅ Implemented" where evidence exists.
   - Add notes about specific implementation details or capabilities.

3. Update phase completion status and next steps.
   - Mark completed phases in the phase sections with completion indicators.
   - Update "Short-term Goals" to reflect current status and next priorities.
   - Ensure next priority tasks are clearly identified for future work.

4. Update gap analysis and milestones.
   - Recalculate implementation and verification gaps based on updated status.
   - Update milestone completion status based on actual achievements.
   - Revise risk assessments based on current implementation state.

## 5. Update System Requirements

1. Update individual requirement implementation status.
   - Use the `replace_in_file` tool to change "Impl Status" from "Not Implemented" to "Implemented" where evidence exists.
   - Update status to "Partial" where some but not complete implementation exists.
   - Ensure status accurately reflects the actual implementation evidence found.

2. Update requirement summary statistics.
   - Recalculate the implementation status summary table.
   - Update percentages for Implemented, Partial, and Not Implemented categories.
   - Ensure summary statistics match the individual requirement statuses.

3. Add implementation notes where helpful.
   - Include brief notes about specific implementation approaches or capabilities.
   - Reference key implementation components for complex requirements.
   - Note any limitations or partial implementations.

## 6. Create Comprehensive Analysis Report

1. Organize outputs under the root directory `outputs/project-status/`.
   - Create a comprehensive analysis report as a new markdown file.
   - Use a descriptive filename like `phase1_completion_analysis.md` or `project_status_analysis.md`.

2. Structure the analysis report with the following sections:
   - **Executive Summary**: High-level findings about actual vs. documented status
   - **Key Findings**: Major discrepancies discovered and overall progress metrics
   - **Requirements Analysis**: Detailed breakdown of implementation and verification status by requirement
   - **Implementation Highlights**: Summary of major implemented features and capabilities
   - **Documentation Updates Completed**: List of all documentation files updated and changes made
   - **Phase Status Assessment**: Analysis of phase completion and readiness for next phase
   - **Recommendations**: Next steps and priorities based on current status

3. Include specific evidence and metrics.
   - Provide concrete examples of implementation evidence found.
   - Include before/after statistics showing documentation corrections.
   - Reference specific files, test cases, and implementation components.
   - Quantify the scope of documentation updates made.

4. Document the verification methodology.
   - Explain how implementation evidence was gathered and verified.
   - Describe the criteria used for determining implementation and verification status.
   - Note any limitations or assumptions made during the analysis.

5. Provide actionable next steps.
   - Clearly identify the next priority tasks based on updated status.
   - Recommend specific actions for maintaining documentation accuracy.
   - Suggest process improvements to prevent documentation drift.

6. Use the `attempt_completion` tool to present the final results.
   - Include the path to the generated analysis report.
   - Provide a concise summary of major findings and corrections made.
   - Include key metrics showing the scope of documentation updates.
   - Highlight any significant achievements or phase completions discovered.
   - Reference the updated documentation files for stakeholder review.

</detailed_sequence_steps>

</task>
