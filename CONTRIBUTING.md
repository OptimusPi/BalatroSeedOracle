# Contributing to Balatro Seed Oracle

Thank you for your interest in contributing to Balatro Seed Oracle! This is a community project, and we welcome contributions of all kinds.

## Ways to Contribute

- **New filter configurations** - Share effective seed hunt strategies
- **Performance improvements** - Optimization suggestions
- **Bug reports** - Issues with specific filters or seeds
- **Feature requests** - Additional filtering capabilities
- **Documentation** - Improve guides, fix typos, add examples
- **Code contributions** - Bug fixes, new features, refactoring

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [git](https://git-scm.com/downloads)
- [go-task](https://taskfile.dev/) (recommended)
- [lefthook](https://github.com/evilmartians/lefthook) (for git hooks)
- [dprint](https://dprint.dev/) (for formatting)

### Setup

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/BalatroSeedOracle.git
   cd BalatroSeedOracle
   ```
3. Run setup:
   ```bash
   task setup
   ```
4. Install formatting tools and hooks:
   ```bash
   # macOS
   brew install lefthook dprint

   # Then install git hooks
   lefthook install
   ```

## Development Workflow

### Making Changes

1. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. Make your changes following the project conventions (see below)

3. **Update CHANGELOG.md** if your changes are user-facing:
   - Add an entry under `## [Unreleased]` in the appropriate category (`Added`, `Changed`, `Fixed`)
   - See `.cursor/rules/005-changelog-policy.mdc` for detailed guidelines
   - Skip this step only for internal changes (CI config, refactoring, etc.)

4. Test your changes:
   ```bash
   task test
   task run:desktop
   ```

5. Format your code:
   ```bash
   task format
   ```

   Or let the pre-commit hooks handle it automatically when you commit.

6. Commit your changes:
   ```bash
   git add .
   git commit -m "Brief description of your changes"
   ```

7. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

8. Open a Pull Request on GitHub

### Code Style

- **C# Code**: Formatted with [CSharpier](https://csharpier.com/) (config: `.config/csharpierrc`)
- **Markdown/JSON/YAML**: Formatted with [dprint](https://dprint.dev/) (config: `.config/dprint.json`)
- **EditorConfig**: Follow `.editorconfig` settings for baseline whitespace/newline rules

Formatting is automatically applied via pre-commit hooks when you commit.

### Project Structure

- `src/BalatroSeedOracle/` - Core application (shared library)
- `src/BalatroSeedOracle.Desktop/` - Desktop-specific code
- `src/BalatroSeedOracle.Browser/` - Browser/WASM-specific code
- `external/Motely/` - Search engine submodule (read-only)
- `docs/` - Documentation
- `.cursor/` - Cursor IDE configuration (rules, skills, commands)

### Coding Conventions

- **MVVM Pattern**: ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- **Dependency Injection**: Use constructor injection for services
- **Logging**: Use `DebugLogger` helper (see `.cursor/rules/010-csharp-logging.mdc`)
- **Async/Await**: Follow project patterns (see `.cursor/rules/020-csharp-async-await.mdc`)
- **Platform Guards**: Use `#if` guards for platform-specific code (see `.cursor/rules/030-platform-guards.mdc`)

### Testing

The project includes unit tests in `tests/BalatroSeedOracle.Tests/`. Tests are required to pass before PRs can be merged.

Run tests with:

```bash
task test
```

Or manually:

```bash
dotnet test tests/BalatroSeedOracle.Tests/BalatroSeedOracle.Tests.csproj -c Release
```

When adding new features or fixing bugs:

- Add tests for new functionality in core services
- Focus on testing pure logic (services, helpers, data transformations)
- Use xUnit for test framework, Moq for mocking dependencies
- Test files should mirror the structure of the code they test

### Building

Desktop:

```bash
task run:desktop        # Release build
task run:desktop:debug  # Debug build
```

Browser:

```bash
task run:browser          # Dev server
task publish:browser      # Production build
```

## Motely Submodule

The `external/Motely/` directory is a git submodule and is **read-only** in this repository.

If you need to modify Motely:

1. Open `external/Motely` as a separate workspace
2. Create a branch and commit in the submodule repo
3. Push to your fork and open a PR to [OptimusPi/MotelyJAML](https://github.com/OptimusPi/MotelyJAML)
4. After the PR is merged, update the parent repo:
   ```bash
   cd external/Motely && git pull
   cd ../.. && git add external/Motely
   git commit -m "Bump Motely submodule to [commit SHA]"
   ```

## Pull Request Guidelines

- **One feature per PR**: Keep changes focused and reviewable
- **Descriptive titles**: Clearly describe what the PR does
- **Link issues**: Reference related issues with `Fixes #123` or `Relates to #456`
- **Update CHANGELOG.md**: Add entries under `## [Unreleased]` for user-facing changes (see `.cursor/rules/005-changelog-policy.mdc`)
- **Update documentation**: Include doc updates for user-facing changes
- **Test your changes**: Ensure the app builds and runs on your platform
- **Follow conventions**: Adhere to project code style and patterns
- **Use PR template**: Fill out the pull request template checklist

## Reporting Issues

When reporting bugs, please include:

- **Description**: Clear description of the issue
- **Steps to reproduce**: Exact steps to trigger the bug
- **Expected behavior**: What you expected to happen
- **Actual behavior**: What actually happened
- **Environment**: OS, .NET version, app version
- **Logs**: Relevant error messages or stack traces

## Questions?

- **Discord**: Balatro community server (#tools channel)
- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For questions and general discussion

Thank you for contributing! 🎉
