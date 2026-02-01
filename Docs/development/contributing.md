# Contributing to PoshUI

Thank you for your interest in contributing to PoshUI! We welcome contributions from the community to help make this framework better for IT professionals everywhere.

## How Can I Contribute?

### Reporting Bugs
If you find a bug, please check the [existing issues](https://github.com/asolutionit/PoshUI/issues) to see if it has already been reported. If not, open a new issue and include:
- A clear, descriptive title.
- Steps to reproduce the problem.
- Expected vs. actual behavior.
- PowerShell and Windows version information.

### Suggesting Enhancements
We love new ideas! If you have a suggestion for a new control, feature, or improvement:
1. Search the issues to see if it's already being discussed.
2. Open a new issue with the "enhancement" label.
3. Describe the use case and how it would benefit other users.

### Code Contributions
1. Fork the repository.
2. Create a new branch for your feature or fix.
3. Follow the [Coding Standards](#coding-standards) below.
4. Submit a Pull Request (PR) with a detailed description of your changes.

---

## Coding Standards

### PowerShell Standards
- **Naming**: Use standard `Verb-Noun` pairs for all public functions.
- **Prefixes**: Use the `UI` prefix for nouns (e.g., `Add-UITextBox`).
- **Splatting**: Use hashtable splatting in examples for readability.
- **Compatibility**: Ensure all code is compatible with Windows PowerShell 5.1.
- **ASCII Only**: Do not use Unicode emojis in `.ps1` files; use Segoe MDL2 glyphs or HTML entities instead.

### C# Standards
- **Dependencies**: Do not add any third-party NuGet packages or DLLs.
- **Framework**: Code must target .NET Framework 4.8.
- **Style**: Follow standard Microsoft C# coding conventions.
- **MVVM**: Maintain the Model-View-ViewModel pattern used in the Launcher project.

---

## Development Environment Setup

1. Follow the [Building from Source](./building-from-source.md) guide.
2. Use the `Examples/` directory to create test scripts for your new features.
3. Run existing examples to ensure no regressions were introduced.

## Pull Request Process

1. Ensure your code builds successfully in Visual Studio (Release configuration).
2. Update the relevant documentation in the `Docs/` folder.
3. Add a summary of your changes to the `CHANGELOG.md`.
4. A maintainer will review your PR and provide feedback.

Next: [Extending PoshUI](./extending.md)
