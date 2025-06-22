<task name="Generate Repository Instructions for AI Assistants">

<task_objective>
This workflow creates a comprehensive repo_instructions.md file that provides AI assistants with detailed guidance for working within any repository. The workflow analyzes the current directory structure to understand the project type, technologies, and patterns, then adapts content based on the intended AI assistant use case.
</task_objective>

<detailed_sequence_steps>
# Generate Repository Instructions Process - Detailed Sequence of Steps

## 1. Determine AI Assistant Use Case

1. Ask the user what type of AI assistant work they want to optimize for.
    - Use the `ask_followup_question` tool to present use case options.
    - Options: "Development-focused (coding, debugging, features)", "Review-focused (code reviews, architecture analysis)", "Documentation-focused (writing docs, reports)", "Project Management (tracking, planning)", "Compliance/Audit (security, standards)", "General/Multi-purpose (comprehensive coverage)"

2. Store the user's selection to guide content emphasis in later steps.

## 2. Discover Repository Structure and Project Type

1. Use the `list_files` tool to get the complete repository structure.
    - Set recursive to true to understand the full project layout.
    - Analyze file extensions and directory names to identify project type.

2. Identify project type and technology stack by examining key indicators.
    - Look for package.json (Node.js), requirements.txt (Python), *.csproj (C#/.NET), pom.xml (Java), Cargo.toml (Rust), go.mod (Go), etc.
    - Check for framework-specific files like angular.json, vue.config.js, next.config.js, etc.
    - Identify build tools: Makefile, Dockerfile, docker-compose.yml, etc.

3. Use the `list_code_definition_names` tool on key source directories.
    - Analyze main source directories (src/, lib/, app/, etc.) to understand code structure.
    - Analyze test directories to understand testing patterns.

## 3. Extract Project Information from Documentation

1. Read and analyze existing documentation files.
    - Use `read_file` tool on README.md if it exists to extract project purpose and setup instructions.
    - Use `read_file` tool on CHANGELOG.md or HISTORY.md if they exist to understand release patterns.
    - Use `read_file` tool on LICENSE or LICENSE.md to understand licensing.
    - Use `read_file` tool on CONTRIBUTING.md if it exists to understand contribution guidelines.

2. Look for additional documentation directories.
    - Check for docs/, documentation/, or similar directories.
    - Read key documentation files to understand project scope and architecture.

## 4. Analyze Configuration and Build Files

1. Examine project configuration files based on detected technology stack.
    - For Node.js: Read package.json, package-lock.json, tsconfig.json, webpack.config.js, etc.
    - For Python: Read requirements.txt, setup.py, pyproject.toml, environment.yml, etc.
    - For .NET: Read *.sln, *.csproj, appsettings.json, etc.
    - For Java: Read pom.xml, build.gradle, application.properties, etc.
    - For other technologies: Read relevant configuration files.

2. Analyze build and deployment configurations.
    - Read Dockerfile, docker-compose.yml if present.
    - Read CI/CD configuration files (.github/workflows/, .gitlab-ci.yml, azure-pipelines.yml, etc.).
    - Look for deployment scripts and configuration.

## 5. Examine Code Architecture and Patterns

1. Analyze key source code files to understand architecture.
    - Read main entry points (main.py, index.js, Program.cs, main.go, etc.).
    - Examine directory structure to understand architectural patterns (MVC, microservices, monolith, etc.).
    - Look for configuration files that indicate frameworks or patterns used.

2. Examine testing patterns and structure.
    - Analyze test directories and files to understand testing approaches.
    - Look for test configuration files (jest.config.js, pytest.ini, etc.).
    - Check for integration, unit, and end-to-end test patterns.

## 6. Discover Development Commands and Workflows

1. Search for development scripts and commands.
    - Use `search_files` tool to find common development commands in documentation and scripts.
    - Look for npm scripts in package.json, make targets in Makefile, etc.
    - Search for build, test, run, deploy commands in documentation.

2. Identify development workflow patterns.
    - Look for git hooks, pre-commit configurations.
    - Check for linting and formatting configurations (.eslintrc, .prettierrc, .editorconfig, etc.).
    - Examine dependency management patterns.

## 7. Generate Repository Instructions Content

1. Create the outputs directory if it doesn't exist.
    - Use `list_files` tool to check if outputs/ directory exists.
    - Use `execute_command` tool to create directory if needed (adjust command for OS).

2. Generate the repo_instructions.md file with adaptive content based on discovered project characteristics and selected use case.
    - Use `write_to_file` tool to create outputs/repo_instructions.md.
    - Adapt content structure based on project type and user's selected use case.

    **Content Structure (adapt sections based on project type):**
    i. Project Overview and Purpose (from README and analysis)
    ii. Technology Stack and Dependencies (from configuration files)
    iii. Repository Structure and Navigation (from directory analysis)
    iv. Development Environment Setup (from documentation and configuration)
    v. Build, Test, and Deployment Commands (from discovered scripts and documentation)
    vi. Architecture and Design Patterns (from code analysis)
    vii. Coding Standards and Conventions (from configuration files and code patterns)
    viii. Testing Strategies and Patterns (from test analysis)
    ix. Key Document References (from discovered documentation)
    x. Development Workflow and Processes (from CI/CD and git configuration)
    xi. Common Tasks and Operations (based on project type)
    xii. Troubleshooting and Known Issues (from documentation)
    xiii. AI Assistant Specific Guidance (tailored to selected use case and project type)

    **Use Case Specific Emphasis:**
    - **Development-focused**: Detailed coding patterns, debugging workflows, common development tasks
    - **Review-focused**: Code quality standards, architecture decisions, review criteria
    - **Documentation-focused**: Documentation standards, key concepts, maintenance guidelines
    - **Project Management**: Workflow processes, dependencies, planning considerations
    - **Compliance/Audit**: Security patterns, standards adherence, regulatory requirements
    - **General**: Comprehensive coverage of all aspects

## 8. Complete Repository Instructions Generation

1. Use the `attempt_completion` tool to present the final result.
    - Inform the user that the repo_instructions.md file has been created in the outputs/ directory.
    - Summarize the discovered project type, key technologies, and how the instructions were tailored.
    - Note that the file provides comprehensive guidance for AI assistants working in this repository.

</detailed_sequence_steps>

</task>
