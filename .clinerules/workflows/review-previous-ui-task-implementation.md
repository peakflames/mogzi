<task name="Review Previous UI Task Implementation">

<task_objective>
Conduct a focused review of a recently completed UI task implementation to identify critical gaps between documented requirements and actual implementation, then provide actionable guidance for completion and process improvements to prevent similar issues. The workflow prioritizes practical next steps and team guidance over exhaustive analysis.
</task_objective>

<detailed_sequence_steps>
# Review Previous UI Task Implementation Process - Detailed Sequence of Steps

## 1. Identify and Analyze Task Context

1. Use the `ask_followup_question` tool to ask the user which specific UI task they want reviewed.
   - Request the task ID or description (e.g., "task 2.1", "AppComponent implementation")
   - Ask for any specific areas of concern or focus for the review

2. Read the project tracker to understand the task context and status.
   - Use the `read_file` tool to examine `docs/features/ui/project_tracker.md`
   - Identify the specific task, its stated completion status, and success criteria
   - Note the task's position in the overall project timeline

3. Read the current status documentation to understand recent changes.
   - Use the `read_file` tool to examine `docs/features/ui/current_status_and_next_steps.md`
   - Understand what was reported as completed and any known issues
   - Identify the scope of files that were modified or created

4. Determine the scope of the review based on the task context.
   - Identify which components, files, and tests should be examined
   - Understand the task's dependencies and integration points
   - Establish the review boundaries and focus areas

## 2. Review Documentation and Requirements

1. Read all relevant UI documentation to understand requirements.
   - Use the `read_file` tool to examine `docs/features/ui/architecture_and_design.md`
   - Use the `read_file` tool to examine `docs/features/ui/component_requirements.md`
   - Use the `read_file` tool to examine `docs/features/ui/concept_of_operations.md`
   - Use the `read_file` tool to examine `docs/features/ui/implementation_plan.md`
   - Use the `read_file` tool to examine `docs/features/ui/test_case_specification.md`

2. Extract specific requirements for the task being reviewed.
   - Identify functional requirements and acceptance criteria
   - Note performance requirements and constraints
   - Document architectural requirements and design patterns
   - Understand integration requirements with other components

3. Analyze the expected vs. actual implementation scope.
   - Compare what was documented as the task scope vs. what was implemented
   - Identify any scope creep or scope reduction
   - Note any requirements that may have been misunderstood or overlooked

## 3. Analyze Test Implementation

1. Examine the test directory structure for the reviewed component.
   - Use the `list_files` tool to explore `test/UI.Tests/` directory
   - Identify test files related to the implemented task
   - Understand the test organization and naming conventions

2. Review test files for completeness and quality.
   - Use the `read_file` tool to examine relevant test files
   - Assess test coverage of the implemented functionality
   - Evaluate test quality, clarity, and maintainability
   - Check if tests follow established patterns and conventions

3. Analyze test alignment with requirements.
   - Verify that tests validate the documented requirements
   - Check if acceptance criteria are properly tested
   - Identify any missing test scenarios or edge cases
   - Assess whether tests follow black-box testing principles

4. Execute tests to verify current status.
   - Use the `execute_command` tool to run the relevant test suite
   - Document test results and any failures
   - Assess test execution performance and reliability

## 4. Review Actual Implementation

1. Examine the source code structure for the implemented components.
   - Use the `list_files` tool to explore `src/UI/` directory structure
   - Use the `list_code_definition_names` tool to understand code organization
   - Identify all files that were created or modified for the task

2. Review individual implementation files for quality and compliance.
   - Use the `read_file` tool to examine each relevant source file
   - Assess code quality, readability, and maintainability
   - Check adherence to established coding patterns and conventions
   - Evaluate error handling and edge case management

3. Analyze architectural compliance and integration.
   - Verify that the implementation follows the documented architecture
   - Check proper use of dependency injection and service patterns
   - Assess integration with existing components and systems
   - Evaluate adherence to the component-based design principles

4. Search for related code patterns and consistency.
   - Use the `search_files` tool to find similar implementations or patterns
   - Check consistency with existing codebase conventions
   - Identify any code duplication or missed reuse opportunities
   - Assess naming conventions and code organization

## 5. Assess Implementation Completeness

1. Compare implementation against documented requirements.
   - Create a detailed mapping of requirements to implementation
   - Identify fully implemented, partially implemented, and missing features
   - Assess the quality of placeholder implementations vs. full functionality
   - Note any deviations from the original specifications

2. Evaluate technical debt and design decisions.
   - Identify areas where shortcuts were taken or technical debt was introduced
   - Assess the impact of design decisions on future development
   - Note any architectural compromises or temporary solutions
   - Evaluate the maintainability and extensibility of the implementation

3. Analyze integration readiness and dependencies.
   - Assess how well the implementation integrates with existing systems
   - Identify any missing dependencies or integration points
   - Evaluate the readiness for the next phase of development
   - Check for any blocking issues or prerequisites

## 6. Focus on Critical Gaps and Actionable Insights

1. **CRITICAL ASSESSMENT**: Distinguish between structural foundation and functional implementation.
   - Identify if the task represents placeholder/structural work vs. complete functional implementation
   - Assess whether "completed" status accurately reflects the documented requirements
   - Focus on the most critical missing functionality that blocks progress

2. **PRACTICAL COMPLETION ASSESSMENT**: Evaluate actual vs. claimed completion percentage.
   - Compare claimed completion status against documented requirements
   - Provide realistic completion percentage based on functional requirements
   - Identify the gap between structural implementation and functional requirements

3. **INTEGRATION READINESS**: Assess readiness for next development phase.
   - Check if required service integrations are implemented (not just referenced)
   - Verify that architectural components are actually connected and functional
   - Identify blocking dependencies that prevent forward progress

4. **ACTIONABLE PRIORITIZATION**: Focus on what needs to be done next.
   - Prioritize missing functionality by impact and dependency order
   - Provide realistic time estimates for completing missing work
   - Identify the critical path for achieving actual task completion

## 7. Provide Team Guidance and Process Improvements

1. **IMMEDIATE NEXT STEPS**: Present focused, actionable guidance.
   - Provide specific, prioritized tasks to complete the reviewed implementation
   - Include realistic time estimates based on actual scope of missing work
   - Focus on critical functionality that enables forward progress

2. **PROCESS IMPROVEMENT RECOMMENDATIONS**: Address root causes to prevent recurrence.
   - **Requirements Verification**: Recommend verification steps before marking tasks complete
   - **Functional vs. Structural Testing**: Distinguish between testing structure vs. testing functionality
   - **Integration Validation**: Ensure components actually integrate with required services
   - **Acceptance Criteria Review**: Verify all documented acceptance criteria are met
   - **Quality Gates**: Recommend specific checkpoints for task completion

3. **DOCUMENTATION UPDATES**: Update project tracking to reflect reality.
   - **ALWAYS** ask user about updating documentation with corrected status and guidance
   - Use the `ask_followup_question` tool to confirm documentation updates
   - Update both `docs/features/ui/current_status_and_next_steps.md` AND `docs/features/ui/project_tracker.md`
   - Correct completion status, progress percentages, and time estimates
   - Add specific notes about missing functionality and remaining work

4. **FOCUSED PRESENTATION**: Present concise, actionable findings.
   - **Executive Summary**: Brief assessment of actual vs. claimed completion
   - **Critical Gaps**: Top 3-5 missing elements that block progress
   - **Immediate Priorities**: Specific next steps with time estimates
   - **Process Improvements**: Concrete recommendations to prevent similar issues
   - **Updated Documentation**: Corrected project tracking and realistic guidance

5. **TEAM-ORIENTED GUIDANCE**: Provide constructive direction for improvement.
   - Focus on helping the team succeed rather than exhaustive criticism
   - Provide clear, actionable steps for completing the work
   - Include process improvements to prevent similar gaps in future tasks
   - Ensure documentation accurately reflects reality for future planning

## 8. Complete Documentation Updates

1. **MANDATORY DOCUMENTATION CORRECTION**: Always offer to update project tracking.
   - Update `docs/features/ui/current_status_and_next_steps.md` with corrected status and next steps
   - Update `docs/features/ui/project_tracker.md` with accurate completion status and progress
   - Provide realistic time estimates and specific missing functionality notes
   - Ensure future development sessions have accurate information

2. **PROCESS IMPROVEMENT INTEGRATION**: Include lessons learned in documentation.
   - Add process improvement recommendations to prevent similar issues
   - Include quality gates and verification steps for future task completion
   - Provide guidance on distinguishing structural vs. functional implementation

3. Use the `attempt_completion` tool to present the final results.
   - Summarize the key gaps identified and corrected documentation
   - Highlight the most critical next steps for task completion
   - Confirm that project tracking now accurately reflects implementation status
   - Provide clear guidance for both completing current work and improving future processes

</detailed_sequence_steps>

</task>
