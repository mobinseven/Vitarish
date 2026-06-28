#!/usr/bin/env python3
"""Validate plan file structure and content."""

import json
import sys
from pathlib import Path
import jsonschema

def validate_plan(plan_path: str) -> bool:
    """Validate a plan file against the schema."""
    plan_file = Path(plan_path)
    schema_file = Path('.mimocode/schemas/plan-schema.json')
    
    if not plan_file.exists():
        print(f"Plan file not found: {plan_path}")
        return False
    
    if not schema_file.exists():
        print(f"Schema file not found: {schema_file}")
        return False
    
    with open(plan_file) as f:
        plan = json.load(f)
    
    with open(schema_file) as f:
        schema = json.load(f)
    
    try:
        jsonschema.validate(plan, schema)
        print(f"Plan file valid: {plan_path}")
        return True
    except jsonschema.ValidationError as e:
        print(f"Plan file invalid: {e.message}")
        return False

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: validate_plan.py <plan_file>")
        sys.exit(1)
    
    success = validate_plan(sys.argv[1])
    sys.exit(0 if success else 1)