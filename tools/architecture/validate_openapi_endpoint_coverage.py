#!/usr/bin/env python3
"""Validate that endpoint mappings use approved OpenAPI-aware conventions."""
from __future__ import annotations
import re, sys
from dataclasses import dataclass
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
SCAN_PATTERNS = [
 'src/Hosts/Main/Program.cs',
 'src/Interfaces/**/Extensions/*Endpoint*.cs',
 'src/Modules/**/Api/*.cs',
 'src/Modules/**/Module/Endpoints/*.cs',
 'src/Modules/**/Infrastructure/Extensions/*Endpoint*.cs',
]
APPROVED = {'MapModuleGet','MapModulePost','MapModulePut','MapModuleDelete','MapModulePatch','MapModuleHead','MapProtectedModuleGroup','MapAnonymousModuleGroup'}
RAW = {'MapGet','MapPost','MapPut','MapDelete','MapPatch','MapMethods','MapGroup'}
ALLOW_RAW_FILES = {'src/Modules/Base/Endpoints/ModuleEndpointMappingExtensions.cs','src/Modules/Base/Endpoints/ProtectedModuleEndpointRouteBuilderExtensions.cs'}
@dataclass
class V:
    path: Path; line: int; kind: str; expr: str; fix: str
def line(text,o): return text.count('\n',0,o)+1
def files():
    found=[]
    for pat in SCAN_PATTERNS:
        found.extend(p.relative_to(ROOT) for p in ROOT.glob(pat) if p.is_file())
    return sorted(set(found), key=lambda p:p.as_posix())
def scan(p:Path):
    if p.as_posix() in ALLOW_RAW_FILES: return []
    text=(ROOT/p).read_text(errors='ignore')
    out=[]
    for m in re.finditer(r'\.\s*(MapGet|MapPost|MapPut|MapDelete|MapPatch|MapMethods|MapGroup)\s*\(', text):
        method=m.group(1)
        if method in RAW:
            window = text[m.start():m.start()+2000]
            if ('ExcludeFromOpenApiWithJustification' in window or 'OpenApiExclusionJustification:' in window):
                continue
            out.append(V(p,line(text,m.start()),'RawEndpointMapping',method,'Use MapProtectedModuleGroup/MapAnonymousModuleGroup and MapModule* helpers, or ExcludeFromOpenApiWithJustification for non-module endpoints.'))
    for m in re.finditer(r'MapAnonymousModuleGroup\((?P<args>[^)]*)\)', text):
        strings=re.findall(r'"([^"]*)"', m.group('args'))
        if len(strings)<2 or not strings[1].strip():
            out.append(V(p,line(text,m.start()),'AnonymousJustificationMissing','MapAnonymousModuleGroup','Provide a non-empty machine-readable justification.'))
    for m in re.finditer(r'MapModule(?:Get|Post|Put|Delete|Patch|Head)\((?P<args>[^;]*)\)', text, re.S):
        args=m.group('args')
        if not re.search(r'"[A-Za-z][A-Za-z0-9]+"', args):
            out.append(V(p,line(text,m.start()),'OperationIdMissing','MapModule*','Pass a stable operation ID such as UsersSearchUsers.'))
    for m in re.finditer(r'ExcludeFromDescription\s*\(', text):
        if 'ExcludeFromOpenApiWithJustification' not in text[max(0,m.start()-200):m.start()+200] and 'OpenApiExclusionJustification:' not in text[max(0,m.start()-200):m.start()+200]:
            out.append(V(p,line(text,m.start()),'OpenApiExclusionJustificationMissing','ExcludeFromDescription','Use ExcludeFromOpenApiWithJustification with a non-empty reason.'))
    return out
violations=[]
for p in files(): violations.extend(scan(p))
print('OpenAPI endpoint coverage validation')
print('Violations found:', len(violations))
for v in violations:
    print(f'{v.path}:{v.line}: kind={v.kind}; endpoint={v.expr}; remediation={v.fix}')
sys.exit(1 if violations else 0)
