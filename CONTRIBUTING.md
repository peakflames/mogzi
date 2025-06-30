# Contributing to MaxBot

Thank you for your interest in contributing to MaxBot! This document provides guidelines and instructions for contributing to the project.

## Development Setup

1. Fork the repository

1. Clone the repository

   ```bash
   git clone https://github.com/peakflames/maxbot.git
   cd maxbot
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
   src/MaxBot.TUI

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
2. Ensure the workflow `.clinerules/workflows/start-new-task.md` is enabled. [Lean more here](https://docs.cline.bot/features/slash-commands/workflows)
2. Boostrap your Cline task session with context by running the `/start-new-task.md`. Once loaded, Cline will ask for the task objective.

## Coding Standards

### Component Development

1. File Organization:
   - One component per file
   - Place in appropriate feature folder under Components/
   - Use partial classes for complex components

1. Naming Conventions:
   - PascalCase for public members and types
   - _camelCase for private fields
   - Parameters must be public properties

1. Documentation:
   - XML comments for public APIs
   - Include parameter descriptions
   - Document event callbacks

1. Code Style:
   - Use 4 spaces for indentation
   - Prefer C# code in .cs file over .razor file
   - Prefer functions over lambda expressions for event handlers

## Getting Help

- Create an issue for bugs
- Start a discussion for features
- Tag maintainers for urgent issues

## License

By contributing, you agree that your contributions will be licensed under as defined by [LICENSE.md](LICENSE.md).