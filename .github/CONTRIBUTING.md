# Contributing

Thank you for your interest in contributing to the SQL Race API Examples.

## Guidelines

### Snippets (Tier 1)

- Must be self-contained in a single file
- Must include all `using` statements and a `Main` method
- Must reference only `MESL.SQLRace.API` — no other dependencies
- Must be under 80 lines
- Must use domain-neutral parameter names (not motorsport-specific)
- Must default to a local SQLite connection string
- Must include a header comment block with title, description, prerequisites, and expected output

### Projects (Tier 2)

- Must build and run with `dotnet run`
- Should use the shared Core library where appropriate
- Must follow the same naming conventions as snippets

### General

- Use modern C# 12 / .NET 8 conventions
- Use `using` declarations for all `IDisposable` resources
- Never hard-code server names, file paths, or session GUIDs
- Write helpful error messages for null/not-found cases

## Submitting Changes

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## Reporting Issues

Use the [issue templates](ISSUE_TEMPLATE/) to report bugs, request examples, or suggest industry-specific scenarios.
