# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1]

- TBD

## [1.2.0]

__Added__:

- Command-line argument `--tool-approvals` (alias `-ta`) to override the tool approval setting from the configuration file.

__Changed__:

- The `FileSystemTools` now respect the `ToolApprovals` setting from the configuration.
- The system prompt now includes the active mode.

## [1.1.1] - 2025-06-20

__Changed__:

- The `--status` command now displays a detailed table of all available providers and profiles, in addition to the active configuration.

## [1.1.0] - 2025-06-20

__Added__:

- Ability to pipe standard input into the CLI as part of the prompt.
- Acceptance test script (`test/local_exe_acceptance.ps1`) to automate testing of the published executable.
- Cline workflow for generating releases (`.clinerules/workflows/generate_release.md`).

__Changed__:

- Updated developer guidelines and workflow rules.

## [0.3.0] - 2025-03-31

__Changed__:

- fix display of emojis and tweaked the system prompt
- tweaked the chat shell prompt

## [0.2.0] - 2025-03-30

__Added__:

- Support for AIFunctions (Tools)
  - ListFiles
  - WriteFile
  - ReadFile

__Changed__:

- Improved console output

## [0.1.0] - 2025-03-30

__Added__:

- Initial release
- Support both Oneshot and Interactive Chat interface with streaming AI responses
- Supports only OpenAI-compatible APIs
- Profile-based configuration for easy switching between providers and models
- Cross-platform support (Windows, MacOS, Linux)
- Support locating the configuration file in the current directory or the user's home directory (in that order).
