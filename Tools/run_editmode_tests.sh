#!/usr/bin/env bash
# Headless Unity EditMode Test Runner with Automated Failure Diagnosis
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_BIN="/home/geisha/Unity/Hub/Editor/6000.3.24f1/Editor/Unity"
RESULTS_FILE="/tmp/editmode.xml"
LOG_FILE="/tmp/editmode.log"
TIMEOUT_SECS=1500

# Remove stale lockfile if dead, or warn if editor is live
if [ -f "$PROJECT_DIR/Temp/UnityLockfile" ]; then
    echo "[WARN] Temp/UnityLockfile exists. If Unity Editor is running, close it or use an isolated worktree." >&2
fi

echo "[INFO] Running Unity EditMode tests (timeout: ${TIMEOUT_SECS}s)..."
set +e
timeout "$TIMEOUT_SECS" "$UNITY_BIN" \
    -batchmode -nographics \
    -projectPath "$PROJECT_DIR" \
    -runTests -testPlatform EditMode \
    -testResults "$RESULTS_FILE" \
    -logFile "$LOG_FILE" \
    "$@"
EXIT_CODE=$?
set -e

if [ $EXIT_CODE -eq 0 ]; then
    echo "[SUCCESS] All tests passed!"
    if [ -f "$RESULTS_FILE" ]; then
        python3 -c "
import xml.etree.ElementTree as ET
try:
    tree = ET.parse('$RESULTS_FILE')
    root = tree.getroot()
    print(f\"Total: {root.get('total')} | Passed: {root.get('passed')} | Failed: {root.get('failed')} | Skipped: {root.get('skipped')}\")
except Exception as e:
    pass
"
    fi
    exit 0
elif [ $EXIT_CODE -eq 2 ]; then
    echo ""
    echo "======================================================================"
    echo "[FAILURE] One or more tests failed (Exit Code 2)."
    echo "======================================================================"
    if [ -f "$RESULTS_FILE" ]; then
        python3 -c "
import xml.etree.ElementTree as ET
try:
    tree = ET.parse('$RESULTS_FILE')
    root = tree.getroot()
    total = root.get('total', '?')
    passed = root.get('passed', '?')
    failed = root.get('failed', '?')
    skipped = root.get('skipped', '?')
    print(f\"Summary: Total={total}, Passed={passed}, Failed={failed}, Skipped={skipped}\n\")
    failures = root.findall('.//test-case[@result=\"Failed\"]')
    print(f\"Failing Test Cases ({len(failures)} total):\n\")
    for i, tc in enumerate(failures, 1):
        name = tc.get('fullname') or tc.get('name')
        msg_elem = tc.find('.//failure/message')
        trace_elem = tc.find('.//failure/stack-trace')
        msg = msg_elem.text.strip() if msg_elem is not None and msg_elem.text else 'No message'
        trace = trace_elem.text.strip() if trace_elem is not None and trace_elem.text else ''
        print(f\"  {i}. {name}\")
        for line in msg.splitlines():
            print(f\"     {line}\")
        if trace:
            first_trace = trace.splitlines()[0].strip()
            print(f\"     at {first_trace}\")
        print()
except Exception as e:
    print(f\"Failed to parse test results XML: {e}\")
"
    else
        echo "[ERROR] Results XML file ($RESULTS_FILE) not found!"
    fi
    exit 2
else
    echo "======================================================================"
    echo "[ERROR] Unity exited with unexpected error code: $EXIT_CODE"
    echo "======================================================================"
    if [ -f "$LOG_FILE" ]; then
        echo "Last 40 lines of $LOG_FILE:"
        tail -n 40 "$LOG_FILE"
    fi
    exit $EXIT_CODE
fi
