# Mogzi Project Developer Guidelines

## References

- **Architecture** - `docs/process/03_architecture.md`
- **Design** - `docs/process/04_00_design.md`

## Coding Standard and Conventions

1. All non-test code is enforeced by `src/.editorconfig`. The must MUST be read by developer before making code changes.
2. All compile warnings MUST be addressed.
3. ULTRA IMPORTANT: prefer Global Usings over File Usings. prefer the Result pattern via FluentResult package. prefer designs that avoid null pointers.

## Debugging

- runtime logs are located at `logs located at ~/.mogzi/logs/`
- **HYPER ULTRA IMPORTANT:** `dotnet run` results in a interactive terminal session, therefore when running the application prefer to ask the user to run there tool manually and provide feedback via text, screenshot, or you can inspect the logs

__Root Cause Protocol:__

- After completing root cause analysis of test failures, prefer to prsent the results making it clear if it is a application or test issue to the user and use the `ask_followup_question` tool for the user's confirmation

## Context Window Usage Rule

- If the 'Context Window Usage' provided by the user's environment is >= 50% utilization, then `ask_followup_question` to ask th euser whether to use the `new_task` tool to continue i a new task 

### Testing Philosophy (Systems-First Approach)

__FUNDAMENTAL PRINCIPLE: Systems-Level Testing Primacy__

All testing SHALL adhere to the requirements-based, systems-first philosophy derived from decades of mission critical software development experience. This approach recognizes that the highest value and coverage is achieved by testing software as users experience it, not as developers build it.

#### Core Testing Philosophy

__"Test the System, Not the Components"__

- __Primary Focus__: Test complete user workflows through external interfaces
- __Coverage Strategy__: Maximum code coverage is achieved through realistic usage patterns, not isolated component testing
- __Quality Assurance__: Real-world defects are discovered through systems-level scenarios, not artificial component isolation
- __Validation Approach__: Software quality is measured by how well it serves user needs, not how well individual pieces function in isolation

#### Testing Hierarchy (Mandatory Order)

1. __FIRST: Systems-Level Testing__ - Test the application as users experience it as scoped by the Requirements
2. __SECOND: Integration Testing__ - Test component interactions only after systems coverage is established. Use the `ask_followup` to obtain user approval to create.
3. __LAST: Component Testing__ - Test isolated components only to fill specific gaps identified by systems testing. Use the `ask_followup` to obtain user approval to create.

#### Philosophical Foundations

__User-Centric Validation__: Software exists to serve users. Testing must validate user experiences, not developer abstractions.

__Emergent Behavior Focus__: Complex software behavior emerges from component interactions. Testing individual components cannot validate this emergence.

__Real-World Fidelity__: Testing environments must mirror production environments. Mocking and isolation reduce test fidelity and miss critical integration defects.

__Coverage Efficiency__: Systems-level testing exercises more code paths per test than any other approach, providing superior return on testing investment.

#### Universal Testing Principles

__Black-Box Primacy__: Test behavior through external interfaces. Internal implementation details are irrelevant to user value.

__No-Mocking Default__: Use real dependencies, real file systems, real networks. Mocking is permitted only when external dependencies are unavailable or prohibitively expensive.

__Workflow Completeness__: Every test must represent a complete user workflow from start to finish.

__Failure Realism__: Test failure scenarios as users would encounter them, not as developers might artificially create them.

#### Quality Gates

__Definition of Done__: No feature is complete until systems-level testing demonstrates end-to-end user value.

__Coverage Threshold__: Systems-level testing must achieve primary coverage before any other testing approaches are considered.

__Integration Validation__: All component interactions must be validated through systems-level scenarios before component-level testing is permitted.

#### Anti-Patterns (Prohibited Approaches)

- Testing components in isolation before systems validation
- Mocking dependencies when real alternatives exist
- Writing tests that exercise code paths users cannot reach
- Prioritizing test execution speed over test realism
- Measuring success by component test coverage rather than user workflow coverage

#### Enforcement Philosophy

This is not merely a testing strategy but a quality philosophy. All team members must understand that software quality is measured by user success, not developer convenience. Testing approaches that prioritize developer productivity over user validation are fundamentally misaligned with software quality objectives.

__Rationale__: Twenty years of mission-critical software development has proven that systems-level testing consistently identifies more defects, provides better coverage, and delivers higher confidence than component-focused approaches. This philosophy ensures that testing effort is invested where it provides maximum value to users and maximum confidence in software quality.
