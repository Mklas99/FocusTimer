# Git Hooks Setup Guide for FocusTimer

## What are Git Hooks?
Git hooks are scripts that run automatically at certain points in the Git workflow. They help enforce code quality standards before code is committed.

## Setup Instructions

### For Windows Users:
```powershell
# Copy the pre-commit hook
copy .githooks\pre-commit.bat .git\hooks\pre-commit

# Or run this command in PowerShell:
Copy-Item -Path ".ghooks\pre-commit.bat" -Destination ".git\hooks\pre-commit"
```

### For macOS/Linux Users:
```bash
# Copy the pre-commit hook
cp .githooks/pre-commit .git/hooks/pre-commit

# Make it executable
chmod +x .git/hooks/pre-commit
```

## What the Pre-Commit Hook Does

When you run `git commit`, the hook performs:

1. **Checks for C# files** - Only runs if C# files are being committed
2. **Builds the project** - Ensures code compiles without errors
3. **Runs code analysis** - Checks for style/quality violations
4. **Blocks bad commits** - Prevents committing code that doesn't pass checks

## Usage

Once installed, the hook runs automatically:

```bash
# This will trigger the hook
git commit -m "Your commit message"

# If checks fail, you'll see errors like:
# ✗ Code analysis issues found!

# Fix the issues and try again
git commit -m "Your commit message"

# Bypass hooks (NOT RECOMMENDED):
git commit --no-verify -m "Your commit message"
```

## Bypassing Hooks (When Necessary)

If you absolutely need to commit without running hooks:

```bash
git commit --no-verify -m "Your message"
```

⚠️ **Use sparingly** - These hooks exist to maintain code quality!

## Troubleshooting

### Hook Not Running
- Ensure the hook file has correct line endings (LF, not CRLF for bash)
- On macOS/Linux, verify it's executable: `chmod +x .git/hooks/pre-commit`
- Check Git is configured: `git config core.hooksPath .git/hooks`

### Hook Permissions Error (Windows)
- Run PowerShell as Administrator
- Adjust execution policy if needed: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned`

### Slow Builds in Hook
- The hook rebuilds the entire solution (not just changed files)
- This ensures comprehensive checking
- Builds are cached, so subsequent runs are faster

## Customization

Edit the hook files in `.githooks/` to modify behavior:
- Add your own checks
- Adjust build configuration
- Change what triggers the hook

Remember to keep changes in sync across the team!

---

For more information on Git hooks, see: https://git-scm.com/docs/githooks
