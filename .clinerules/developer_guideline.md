# MaxBot Project Developer Guidelines

## References

- **Architecture** - `docs/process/03_architecture.md`
- **Design** - `docs/process/04_00_design.md`

## Coding Standard and Conventions

1. All non-test code is enforeced by `src/.editorconfig`. The must MUST be read by developer before making code changes.
2. All compile warnings MUST be addressed.
3. ULTRA IMPORTANT: prefer Global Usings over File Usings.

## Debugging

- runtime logs are located at `logs located at ~/.max/logs/`
- prefer have the user run there tool and you look at logs and user provide screenshots of output. Rationale: you have trouble obtaining the outputs from the terminal.

