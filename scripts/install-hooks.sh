#!/bin/bash
# Install git hooks from scripts/hooks/ into .git/hooks/
REPO_ROOT="$(git rev-parse --show-toplevel)"
cp "$REPO_ROOT/scripts/hooks/pre-commit" "$REPO_ROOT/.git/hooks/pre-commit"
chmod +x "$REPO_ROOT/.git/hooks/pre-commit"
echo "Pre-commit hook installed. Tests will run before each commit."
echo "Use 'git commit --no-verify' to bypass."
