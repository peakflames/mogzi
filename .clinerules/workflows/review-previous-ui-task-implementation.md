<task name="Review Previous UI Task Implementation">

<task_objective>
Conduct a systematic READ ONLY review of a recently completed UI task implementation by comparing the actual implementation against documented requirements, analyzing code quality and test coverage, and presenting a comprehensive assessment. The workflow operates in READ ONLY MODE and only updates documentation if explicitly requested by the user.
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

## 6. Organize Assessment Findings (READ ONLY MODE)

1. Organize the assessment findings into a structured analysis.
   - Create a clear executive summary of the review findings
   - Organize detailed findings by category (requirements, code quality, tests, etc.)
   - Provide specific examples and evidence for all assessments
   - Include both positive findings and areas for improvement

2. Prepare the assessment analysis with the following sections:
   - **Executive Summary**: High-level assessment of implementation success
   - **Task Context**: Description of the reviewed task and its scope
   - **Requirements Compliance Analysis**: Detailed comparison of implementation vs. requirements
   - **Implementation Review**: Code quality, architecture compliance, and technical assessment
   - **Test Coverage Evaluation**: Analysis of test completeness and quality
   - **Technical Debt Assessment**: Identification of shortcuts, compromises, and future risks
   - **Integration Analysis**: Assessment of how well the implementation fits with existing systems
   - **Strengths and Achievements**: Positive aspects of the implementation
   - **Gaps and Deficiencies**: Areas where implementation falls short of requirements
   - **Recommendations**: Specific actionable recommendations for improvement
   - **Next Steps**: Suggested priorities for future development

3. Include specific metrics and quantitative assessments where possible.
   - Count of requirements fully/partially/not implemented
   - Test coverage statistics and test execution results
   - Number of files created/modified and lines of code
   - Technical debt indicators and complexity metrics

4. **IMPORTANT**: This workflow operates in READ ONLY MODE - no files are created or modified without explicit user permission.

## 7. Present Findings and Offer Documentation Update

1. Prepare a concise summary of key findings.
   - Highlight the most critical issues and successes
   - Prioritize recommendations based on impact and effort
   - Identify any blocking issues that need immediate attention
   - Suggest the most logical next steps for continued development

2. Present the complete review analysis to the user.
   - Provide a comprehensive executive summary of findings
   - Include detailed analysis of all assessment categories
   - Highlight key strengths and critical gaps
   - Present prioritized recommendations for next steps
   - Reference specific files, tests, and code sections as evidence

3. Ask user about updating documentation (CONDITIONAL FILE EDITING).
   - Use the `ask_followup_question` tool to ask if the user wants to update `docs/features/ui/current_status_and_next_steps.md` with the review findings and recommendations
   - Explain that this would add the assessment results and next steps to the existing documentation
   - Only proceed with file editing if the user explicitly agrees

4. If user agrees to documentation update, update the status file.
   - Use the `read_file` tool to examine the current `docs/features/ui/current_status_and_next_steps.md`
   - Use the `replace_in_file` tool to add a new section with the review findings
   - Include the assessment summary, key recommendations, and next priority tasks
   - Maintain the existing structure and format of the document

5. Use the `attempt_completion` tool to present the final results.
   - Summarize the review analysis performed
   - Indicate whether documentation was updated (if user requested)
   - Provide clear guidance for addressing identified issues
   - Suggest concrete next steps for continued development
   - Ensure the review provides actionable insights for future work

</detailed_sequence_steps>

</task>
