# Contributing to Mogzi

Thank you for your interest in contributing to Mogzi! This document provides guidelines and instructions for contributing to the project.

## Development Setup

1. Fork the repository

1. Clone the repository

   ```bash
   git clone https://github.com/peakflames/mogzi.git
   cd mogzi
   ```

1. Install the required toolchain

   - [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
   - Obtain a the following for an OpenAI-Compatible API Provider like [requesty.ai](https://requesty.ai/):
       - BaseUrl
       - ApiKey
       - ModelId

2. Build and Run the Solution

   ```bash
   # change to the Terminal User Interface (TUI) project directory
   src/Mogzi.TUI

   # build the project
   dotnet build

   # run the project
   dotnet run
   ```

## Development Process

1. The develop is equivalent to that of a TQL-5 defined by DO-330
2. Process artifacts are maintained in `docs/process` directory
3. All key feature shall have associated TORs
4. All key feature shall be verified using Requirements-Based Testing (i.e. Black Box)
5. Unit Tesing are only created when absolutely necessary, as deemed by the project maintainer.
6. Git Flow branching and Pull Requests are used coordinate parallel development (i.e. `feature/add-feature-name`, `hotfix/address-issue-12`)

## Requirements Trace Matrix

The project includes an automated requirements trace matrix generator that tracks which requirements are covered by tests. This tool helps ensure comprehensive test coverage and maintains traceability between requirements and verification.

### Generating the Trace Matrix

To generate the requirements trace matrix:

```bash
# Run the trace matrix generator script
./scripts/generate_trace_matrix.py
```

This will generate two output files:
- `outputs/latest_rqmts_trace_matrix.md` - Markdown format for documentation
- `outputs/latest_rqmts_trace_matrix.html` - HTML format with enhanced styling and dark theme

### Viewing the HTML Report

To view the HTML trace matrix in your browser:

```bash
# Start a local web server in the outputs directory
python -m http.server -d outputs/

# Then open your browser to: http://localhost:8000/latest_rqmts_trace_matrix.html
```

### Adding Test Coverage

To link test cases to specific requirements, add requirement ID comments on the same line as the assertion that validates the requirement:

```csharp
[Fact]
public void MyTestMethod_ShouldValidateRequirement()
{
    // Test implementation here...
    var result = MyMethod();
    result.Should().BeTrue("this is the reason why"); // TOR-1.1, TOR-2.3
}
```

The trace matrix generator automatically scans for these comments and creates the traceability links.

### Coverage Statistics

The trace matrix provides:
- Overall coverage percentage
- Coverage breakdown by requirement priority (Critical, High, Medium, Low)
- Detailed mapping of requirements to test files and methods
- Identification of requirements without test coverage


**Forking Development Workflow:**

1. Coordinate approval to implement fix or feature in the Github issues
2. Sync your fork's `develop` branch wth the latest change from upstream `develop` branch
3. From your fork, create a `feature/` or `hotfix/` branch
4. On your branch:
   1. Make code changes
   2. Update docs
   3. Add tests
5. Create a pull request to merge your fork branch into the upstream `develop` branch

**Pull Request Process:**
- All builds must pass
- Demo pages for new components
- Documentation updates
- No breaking changes without discussion
- Follows coding standards

## Recommended AI Development Workflow

1. Leverage the Cline Extension for Visual Studio Code
2. Ensure the `.clinerules/developer_guideline.md` Rule file is enabled
3. Ensure the workflow `.clinerules/workflows/start-new-task.md` is enabled. [Lean more here](https://docs.cline.bot/features/slash-commands/workflows)
4. Boostrap your Cline task session with context by running the `/start-new-task.md`. Once loaded, Cline will ask for the task objective.

## Coding Standards

The project uses `.clinerules/developer_guideline.md` to define and enforce the project conding standards and conventions.

## Getting Help

- Create an issue for bugs
- Start a discussion for features
- Tag maintainers for urgent issues

## License

By contributing, you agree that your contributions will be licensed under as defined by [LICENSE.md](LICENSE.md).
