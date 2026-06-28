#!/usr/bin/env python3
"""Manage loop-state.json lifecycle."""

import json
import os
import sys
from pathlib import Path
from datetime import datetime, timezone
from typing import Optional

ISSUE_NUMBER = os.getenv('ISSUE_NUMBER')
STATE_FILE = Path(f'.mimocode/loop-state-{ISSUE_NUMBER}.json') if ISSUE_NUMBER else Path('.mimocode/loop-state.json')

def init_state(issue_number: int, max_iterations: int = 5):
    """Initialize loop state."""
    now_iso = datetime.now(timezone.utc).isoformat()
    state = {
        "issue_number": issue_number,
        "iteration": 0,
        "max_iterations": max_iterations,
        "status": "planning",
        "history": [],
        "created_at": now_iso,
        "updated_at": now_iso
    }
    
    STATE_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(STATE_FILE, 'w') as f:
        json.dump(state, f, indent=2)
    
    print(f"Initialized loop state for issue #{issue_number}")
    return state

def update_state(status: Optional[str] = None, error: Optional[str] = None, iteration: Optional[int] = None):
    """Update loop state."""
    if not STATE_FILE.exists():
        print("State file not found")
        return None
    
    with open(STATE_FILE) as f:
        state = json.load(f)
    
    if status:
        state['status'] = status
    
    if error is not None:
        state['error'] = error if error != "" else None
    
    if iteration is not None:
        state['iteration'] = iteration
    
    now_iso = datetime.now(timezone.utc).isoformat()
    state['updated_at'] = now_iso
    state['history'].append({
        "iteration": state['iteration'],
        "status": state['status'],
        "timestamp": now_iso
    })
    
    with open(STATE_FILE, 'w') as f:
        json.dump(state, f, indent=2)
    
    print(f"Updated loop state: {status or state['status']}")
    return state

def get_state():
    """Get current loop state."""
    if not STATE_FILE.exists():
        return None
    
    with open(STATE_FILE) as f:
        return json.load(f)

def check_termination():
    """Check if loop should terminate."""
    state = get_state()
    if not state:
        return True, "No state file"
    
    if state['status'] in ['completed', 'failed', 'escalated']:
        return True, f"Terminal state: {state['status']}"
    
    if state['iteration'] >= state['max_iterations']:
        return True, f"Max iterations reached: {state['iteration']}/{state['max_iterations']}"
    
    return False, None

def main():
    if len(sys.argv) < 2:
        print("Usage: loop_state.py <command> [args]")
        print("Commands: init, update, get, check")
        sys.exit(1)
    
    command = sys.argv[1]
    
    if command == 'init':
        if len(sys.argv) < 3:
            print("Usage: loop_state.py init <issue_number>")
            sys.exit(1)
        init_state(int(sys.argv[2]))
    
    elif command == 'update':
        status = None
        error = None
        iteration = None
        for arg in sys.argv[2:]:
            if arg.startswith('--status='):
                status = arg.split('=', 1)[1]
            elif arg.startswith('--error='):
                error = arg.split('=', 1)[1]
            elif arg.startswith('--iteration='):
                iteration = int(arg.split('=', 1)[1])
        update_state(status, error, iteration)
    
    elif command == 'get':
        state = get_state()
        if state:
            print(json.dumps(state, indent=2))
        else:
            print("No state file")
    
    elif command == 'check':
        should_stop, reason = check_termination()
        if 'GITHUB_OUTPUT' in os.environ:
            with open(os.environ['GITHUB_OUTPUT'], 'a') as fh:
                print(f"should_stop={str(should_stop).lower()}", file=fh)
                print(f"reason={reason or ''}", file=fh)
        if should_stop:
            print(f"TERMINATE: {reason}")
            sys.exit(0)
        else:
            print(f"CONTINUE: {reason}")
            sys.exit(0)
    
    else:
        print(f"Unknown command: {command}")
        sys.exit(1)

if __name__ == '__main__':
    main()