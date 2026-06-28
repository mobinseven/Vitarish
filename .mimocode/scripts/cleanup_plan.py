#!/usr/bin/env python3
"""Clean up completed plan files."""

import json
import os
import sys
from pathlib import Path

ISSUE_NUMBER = os.getenv('ISSUE_NUMBER')
STATE_FILE = Path(f'.mimocode/loop-state-{ISSUE_NUMBER}.json') if ISSUE_NUMBER else Path('.mimocode/loop-state.json')
PLANS_DIR = Path('.mimocode/plans')

def cleanup():
    """Remove plan file if loop is completed."""
    if not STATE_FILE.exists():
        print("No state file found")
        return
    
    with open(STATE_FILE) as f:
        state = json.load(f)
    
    if state['status'] != 'completed':
        print(f"Loop not completed (status: {state['status']})")
        return
    
    if 'plan_file' in state:
        plan_file = PLANS_DIR / state['plan_file']
        if plan_file.exists():
            plan_file.unlink()
            print(f"Deleted plan file: {plan_file}")
        else:
            print(f"Plan file not found: {plan_file}")

if __name__ == '__main__':
    cleanup()