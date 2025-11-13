# Changelog

All notable changes to TestRunner will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive documentation (README.md, CONTRIBUTING.md)
- Example configuration files
- Support for multiple output formats (Console, JSON, XML/JUnit, HTML, Markdown, CSV)
- Auto-detection for Ruby projects
- Retry mechanism with configurable delay
- Expected/forbidden output patterns validation
- Priority-based project execution
- Global and project-specific environment variables
- Pre/post execution commands
- Tag-based project filtering
- Dry-run mode for testing configuration

### Changed
- N/A

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [1.0.0] - 2024-11-13

### Added
- Initial release
- Core test runner functionality
- Multi-language project support:
  - JavaScript/TypeScript (Node.js, React, Vue, Angular)
  - Mobile (React Native, Flutter)
  - Python
  - .NET (C#, F#)
  - Java
  - Go
  - Rust
  - PHP
  - Docker
- CLI with four main commands:
  - `init` - Initialize configuration
  - `detect` - Auto-detect projects
  - `validate` - Validate configuration
  - `run` - Execute tests
- Parallel and sequential execution modes
- Configurable timeouts
- Console-based colored output
- JSON configuration format
- Automatic project type detection
- Working directory support
- Custom command execution
- Error handling and logging
- Process management with timeout
- Environment variable injection

### Fixed
- HTML encoding in report generation (System.Web.HttpUtility → System.Net.WebUtility)
- Project filtering logic in dry-run mode
- JsonNamingPolicy configuration (invalid SnakeCaseLower removed)
- Environment.Exit() preventing proper cleanup
- Command injection vulnerability in bash command execution
- Null reference in package.json script parsing
- Shell command detection logic

### Security
- Improved shell command escaping
- Proper single-quote escaping for bash commands
- Working directory validation before command execution
- Executable validation before process start
- Added cancellation token checks
- Enhanced error handling with detailed logging

## Version History

### Version Numbering

We use Semantic Versioning:
- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible functionality additions
- **PATCH** version for backwards-compatible bug fixes

### Upgrade Guide

#### From 0.x to 1.0

No upgrade needed as this is the initial release.

Future upgrade guides will be provided here for major version changes.

---

## Types of Changes

- **Added** for new features.
- **Changed** for changes in existing functionality.
- **Deprecated** for soon-to-be removed features.
- **Removed** for now removed features.
- **Fixed** for any bug fixes.
- **Security** in case of vulnerabilities.

## Release Process

1. Update version in `TestRunner.csproj`
2. Update this CHANGELOG.md with release date
3. Create git tag: `git tag -a v1.0.0 -m "Version 1.0.0"`
4. Push tag: `git push origin v1.0.0`
5. Create GitHub release with changelog
6. Publish to NuGet (if applicable)

## Links

- [Repository](https://github.com/OWNER/TestRunner)
- [Issues](https://github.com/OWNER/TestRunner/issues)
- [Releases](https://github.com/OWNER/TestRunner/releases)
