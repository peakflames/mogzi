<task name="Implementation Progress Assessment">

<task_objective>
Perform a detailed review of implementation progress against architecture and design specifications. This workflow reads architecture and design documents, analyzes the current TUI implementation for compliance, identifies gaps and issues, and generates multiple analysis files with timestamped reports plus an updated Phase 3 implementation plan.
</task_objective>

<detailed_sequence_steps>
# Implementation Progress Assessment Process - Detailed Sequence of Steps

## 1. Document Analysis

1. Use `read_file` to read `docs/process/03_architecture.md` to understand target architecture.

2. Use `read_file` to read `docs/process/04_00_design.md` to understand design specifications.

3. Extract key architectural patterns, design principles, and implementation requirements.

4. Create summary of expected implementation approach based on documentation.

## 2. Understanding Refactoring of Legacy God Loop to new Architecture and Design

1. Use `read_file` to read `docs/refactoring_notes/README.md` to understand the refactoring strategy.

2. Use `read_file` to read `docs/refactoring_notes/implement_phase_1.md` to understand Phase 1 goals and progress.

3. Use `read_file` to read `docs/refactoring_notes/implement_phase_2.md` to understand Phase 2 goals and progress.

4. Analyze the transition plan from legacy god loop to new architecture.

5. Document understanding of the refactoring phases and their objectives.

## 3. User Approval Checkpoint

1. Present brief, to-the-point summary of architecture, design, and refactoring understanding.

2. Provide concise initial assessment of the refactoring approach (appropriateness, potential issues).

3. Use `ask_followup_question` tool to get user confirmation to proceed with implementation analysis.

4. Document user feedback and approval to continue.

## 4. Implementation Analysis

1. Use `list_files` to catalog all files in `src/Mogzi.TUI` with recursive listing.

2. Use `list_code_definition_names` to understand code structure in `src/Mogzi.TUI`.

3. Use `read_file` to read key implementation files:
   - App layer files
   - Components layer files
   - State management files
   - Infrastructure files

4. Compare actual implementation against architecture and design specifications.

5. Document alignment and deviations from planned approach.

## 5. Gap Identification

1. Use `search_files` to find TODO comments, placeholders, and incomplete implementations in `src/Mogzi.TUI`.

2. Use `search_files` to identify potential dead code, unused classes/methods, and duplicated functionality.

3. Compare state management implementation against design specifications.

4. Document specific misalignments between actual and planned implementation.

5. Create prioritized list of implementation issues.

## 6. Deep Analysis Review

1. Re-examine critical findings from previous steps for accuracy.

2. Cross-reference implementation gaps with refactoring phase objectives.

3. Validate assessment conclusions through secondary analysis.

4. Refine and consolidate findings into clear, actionable insights.

5. Ensure analysis is comprehensive and accurate before reporting.

## 7. Report Generation

1. Check if `outputs/` directory exists, create if needed.

2. Generate current timestamp in YYYYMMDD-HHMMSS format.

3. Create `outputs/architecture-review-{timestamp}.md` with:
   - Architecture compliance analysis
   - Alignment assessment with target design
   - Key findings in terse, pithy format

4. Create `outputs/implementation-gaps-{timestamp}.md` with:
   - Specific gap findings
   - Dead code and placeholder identification
   - Prioritized action items in concise format

5. Ensure both reports are to-the-point and actionable.

## 8. Generate/Update Phase 3 Implementation Plan

1. Check if `docs/refactoring_notes/implement_phase_3.md` exists using `read_file`.

2. Use `read_file` to read `src/Mogzi.TUI/App/LegacyFlexColumnTuiApp.cs` to understand the legacy implemenation to be refactored.

3. Based on analysis findings, create or update the Phase 3 implementation file.

4. Define Phase 3 objectives to address identified gaps and complete refactoring.

5. Align Phase 3 goals with overall refactoring strategy from previous phases.

6. Use `write_to_file` to create or update `docs/refactoring_notes/implement_phase_3.md`.

7. Use `attempt_completion` tool to present results with short, terse summary of generated reports and Phase 3 plan.

</detailed_sequence_steps>

</task>
