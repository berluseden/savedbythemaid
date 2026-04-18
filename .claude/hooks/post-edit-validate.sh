#!/bin/bash
# Runs after Edit/Write — validates the changed file
# Receives JSON on stdin: { "tool_name": "Edit", "tool_input": { "file_path": "..." } }

FILE_PATH=$(cat /dev/stdin | python3 -c "
import json, sys
data = json.load(sys.stdin)
print(data.get('tool_input', {}).get('file_path', ''))
" 2>/dev/null)

if [ -z "$FILE_PATH" ]; then exit 0; fi

PROJECT_DIR="${CLAUDE_PROJECT_DIR:-$(pwd)}"

# TypeScript / TSX files
if echo "$FILE_PATH" | grep -qE '\.(ts|tsx)$'; then
  echo "→ Type-checking TypeScript..."
  cd "$PROJECT_DIR/frontend" && npx tsc --noEmit 2>&1 | tail -15
  exit ${PIPESTATUS[0]}
fi

# C# files
if echo "$FILE_PATH" | grep -q '\.cs$'; then
  echo "→ Building .NET..."
  cd "$PROJECT_DIR" && dotnet build backend --no-restore -v quiet 2>&1 | tail -10
  exit ${PIPESTATUS[0]}
fi

exit 0
