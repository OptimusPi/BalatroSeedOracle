# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the latest release version.

| Version | Supported          |
| ------- | ------------------ |
| Latest  | :white_check_mark: |
| < Latest| :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability in Balatro Seed Oracle, please report it responsibly.

### How to Report

1. **Do NOT open a public GitHub issue** for security vulnerabilities
2. Instead, report via one of these methods:
   - **Email**: Contact the maintainer directly (see GitHub profile)
   - **GitHub Security Advisory**: Use the "Security" tab → "Report a vulnerability" (if available)
   - **Discord**: Direct message the maintainer on the Balatro community Discord

### What to Include

Please include the following information in your report:
- **Description**: Clear description of the vulnerability
- **Impact**: Potential impact and attack scenario
- **Steps to reproduce**: Detailed steps to reproduce the issue
- **Affected versions**: Which versions are affected
- **Suggested fix**: If you have one (optional)

### Response Timeline

- **Initial response**: Within 48 hours
- **Status update**: Within 7 days
- **Fix timeline**: Depends on severity and complexity

### Disclosure Policy

- We will acknowledge your report within 48 hours
- We will provide a more detailed response within 7 days
- We will work with you to understand and validate the issue
- Once fixed, we will publicly disclose the vulnerability (with credit to you, if desired)
- We ask that you do not publicly disclose the issue until we have released a fix

## Security Best Practices

### For Users

- **Keep updated**: Always use the latest version
- **Verify downloads**: Download only from official sources (GitHub Releases)
- **Check signatures**: Verify release signatures when available
- **Report issues**: Report suspicious behavior immediately

### For Contributors

- **No secrets in code**: Never commit API keys, passwords, or tokens
- **Review dependencies**: Be cautious with new dependencies
- **Validate input**: Always validate and sanitize user input
- **Follow conventions**: Adhere to secure coding practices in `.cursor/rules/`

## Known Security Considerations

### Local Data Storage

- Search results are stored locally in DuckDB files
- Filter configurations may contain sensitive search criteria
- No data is transmitted to external servers by default

### Submodule Security

- The Motely search engine is included as a git submodule
- Verify submodule integrity when updating: `git submodule status`

### Browser/WASM Build

- Browser builds run in a sandboxed WASM environment
- Threaded builds require COOP/COEP headers (see README)
- No external network access from WASM code

## Contact

For security-related questions or concerns, contact the maintainer via:
- GitHub: [@OptimusPi](https://github.com/OptimusPi)
- Discord: Balatro community server

Thank you for helping keep Balatro Seed Oracle secure!
