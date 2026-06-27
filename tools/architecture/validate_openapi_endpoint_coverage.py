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
 'src/Modules/**/Infrastructure/Api/*.cs',
 'src/Modules/**/Module/Endpoints/*.cs',
 'src/Modules/**/Infrastructure/Extensions/*Endpoint*.cs',
]
RAW = {'MapGet','MapPost','MapPut','MapDelete','MapPatch','MapMethods','MapGroup'}
ALLOW_RAW_FILES = {'src/Modules/Aggregator/Core/Endpoints/ModuleEndpointMappingExtensions.cs','src/Modules/Aggregator/Core/Endpoints/ProtectedModuleEndpointRouteBuilderExtensions.cs'}
CONTRACT_ROOT_RE = re.compile(r'^src/Modules/[^/]+/Contracts/')
FORBIDDEN_SCHEMA_RE = re.compile(r'/((Domain|Application|Infrastructure|Persistence|Identity|Interfaces?|UI|Client|Clients)|Data|Entity|Entities)/|/Sdk/|/Vendor/', re.I)
TYPE_INDEX = None
ALIAS_RE = re.compile(r'^using\s+(?P<alias>[A-Z])\s*=\s*(?P<namespace>[^;]+);', re.M)
PRIMITIVE_TYPES = {'string','String','Guid','bool','Boolean','int','Int32','long','Int64','decimal','Decimal','double','Double','float','Single','DateTime','DateOnly','TimeOnly','Stream','IResult','Results','IFormFile','CancellationToken'}
NO_BODY_STATUSES = {'StatusCodes.Status202Accepted','StatusCodes.Status204NoContent','202','204'}
DEFAULT_STATUS = {'MapModuleGet':'200','MapModulePost':'200','MapModulePut':'200','MapModulePatch':'200','MapModuleHead':'200','MapModuleDelete':'204'}
@dataclass
class V:
    path: Path; line: int; kind: str; expr: str; fix: str
def line(text,o): return text.count('\n',0,o)+1
def statement_end(text,start):
    depth=0; quote=None; escape=False
    for i in range(start,len(text)):
        c=text[i]
        if quote:
            if escape: escape=False
            elif c=='\\': escape=True
            elif c==quote: quote=None
            continue
        if c in ('"',"'"): quote=c
        elif c in '({[': depth+=1
        elif c in ')}]' and depth>0: depth-=1
        elif c==';' and depth==0: return i
    return -1
def files():
    found=[]
    for pat in SCAN_PATTERNS: found.extend(p.relative_to(ROOT) for p in ROOT.glob(pat) if p.is_file())
    return sorted(set(found), key=lambda p:p.as_posix())
def split_args(s):
    args=[]; start=0; depth=0; quote=None; escape=False
    for i,c in enumerate(s):
        if quote:
            if escape: escape=False
            elif c=='\\': escape=True
            elif c==quote: quote=None
        elif c in ('"',"'"): quote=c
        elif c in '(<{[': depth+=1
        elif c in ')>}]' and depth>0: depth-=1
        elif c==',' and depth==0:
            args.append(s[start:i].strip()); start=i+1
    tail=s[start:].strip()
    if tail: args.append(tail)
    return args
def named(args,name):
    for a in args:
        if re.match(rf'^{name}\s*:', a): return a.split(':',1)[1].strip()
    return None
def positional(args,index): return args[index].strip() if len(args)>index and ':' not in args[index].split('(',1)[0] else None
def type_token(expr):
    if not expr: return None
    m=re.search(r'typeof\s*\(\s*([^\)]+)\s*\)', expr)
    return m.group(1).strip() if m else expr.strip()
def build_type_index():
    global TYPE_INDEX
    if TYPE_INDEX is not None: return TYPE_INDEX
    TYPE_INDEX={}
    for f in ROOT.glob('src/**/*.cs'):
        try: text=f.read_text(errors='ignore')
        except OSError: continue
        for m in re.finditer(r'\b(?:class|record|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)', text):
            TYPE_INDEX.setdefault(m.group(1), []).append(f.relative_to(ROOT))
    return TYPE_INDEX
def normalize_type_name(t):
    clean=re.sub(r'[?\[\]]','',t).strip()
    clean=re.sub(r'^(?:IEnumerable|IReadOnlyList|List|Collection|Task|ValueTask|ActionResult)<(.+)>$', r'\1', clean)
    return clean.split('<')[-1].split('.')[-1]
def find_type_file(t):
    if not t: return None
    base=normalize_type_name(t)
    matches=build_type_index().get(base, [])
    contract_matches=[m for m in matches if CONTRACT_ROOT_RE.search(m.as_posix())]
    if contract_matches: return contract_matches[0]
    return matches[0] if len(matches)==1 else None
def schema_ok(t):
    if not t: return True, 'no schema'
    clean=normalize_type_name(t)
    if clean in PRIMITIVE_TYPES: return True, 'primitive'
    fp=find_type_file(clean)
    if fp is None:
        if '.Contracts.' in t: return True, 'contract namespace'
        return False, f'Could not resolve schema type {clean}; use an explicit module Contracts DTO.'
    s=fp.as_posix()
    if CONTRACT_ROOT_RE.search(s): return True, s
    if FORBIDDEN_SCHEMA_RE.search(s): return False, f'{clean} resolves to {s}; OpenAPI schemas must be module Contracts shapes.'
    return False, f'{clean} resolves to {s}; OpenAPI schemas must be module Contracts shapes.'
def scan_handler_request_types(text, handler):
    if not handler or handler.startswith('(') or '=>' in handler: return []
    name=handler.split('.')[-1].strip()
    m=re.search(rf'\b{name}\s*\((?P<params>[^)]*)\)', text)
    if not m: return []
    out=[]
    for part in split_args(m.group('params')):
        part_clean = re.sub('^(\\[[^\\]]+\\]\\s*)+', '', part.strip())
        bits = part_clean.split()
        if len(bits) >= 2:
            typ=bits[0].replace('readonly','').strip()
            param_text=part.strip()
            if typ.startswith('I') and len(typ) > 1 and typ[1].isupper(): continue
            if typ.endswith('DbContext') or typ in {'ClaimsPrincipal','SignInManager<User>','UserManager<User>'}: continue
            if '[AsParameters]' in param_text or '[FromBody]' in param_text or typ.endswith(('Request','Parameters','Dto')):
                if typ not in PRIMITIVE_TYPES and not typ.startswith('Http') and typ != 'ISender': out.append(typ)
    return out
def scan(p:Path):
    if p.as_posix() in ALLOW_RAW_FILES: return []
    text=(ROOT/p).read_text(errors='ignore'); out=[]
    excluded_groups=set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\.ExcludeFromOpenApiWithJustification\s*\(', text))
    for m in re.finditer(r'\.\s*(MapGet|MapPost|MapPut|MapDelete|MapPatch|MapMethods|MapGroup)\s*\(', text):
        method=m.group(1); end=statement_end(text,m.start()); window=text[m.start():end] if end!=-1 else text[m.start():m.start()+2000]
        if 'ExcludeFromOpenApiWithJustification' in window: continue
        statement_start=text.rfind('\n',0,m.start())+1
        prefix=text[statement_start:m.start()]
        receiver=re.search(r'([A-Za-z_][A-Za-z0-9_]*)\s*$', prefix)
        if receiver and receiver.group(1) in excluded_groups: continue
        if method == 'MapGroup' and end != -1:
            statement=text[statement_start:end]
            assigned=re.search(r'\b(?P<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*.*\.\s*MapGroup\s*\(', statement, re.S)
            if assigned and assigned.group('name') in excluded_groups: continue
        out.append(V(p,line(text,m.start()),'RawEndpointMapping',method,'Use MapProtectedModuleGroup/MapAnonymousModuleGroup and MapModule* helpers, or ExcludeFromOpenApiWithJustification for non-module endpoints.'))
    for m in re.finditer(r'MapAnonymousModuleGroup\((?P<args>[^)]*)\)', text):
        strings=re.findall(r'"([^"]*)"', m.group('args'))
        if len(strings)<2 or not strings[1].strip(): out.append(V(p,line(text,m.start()),'AnonymousJustificationMissing','MapAnonymousModuleGroup','Provide a non-empty machine-readable justification.'))
    for m in re.finditer(r'(?P<method>MapModule(?:Get|Post|Put|Delete|Patch|Head))\((?P<args>[^;]*)\)', text, re.S):
        method=m.group('method'); args=split_args(m.group('args')); lineno=line(text,m.start())
        op=named(args,'operationId') or positional(args,2)
        if not op or not re.match(r'"[A-Z][A-Za-z0-9]+"$', op): out.append(V(p,lineno,'OperationIdMissing',method,'Pass a stable PascalCase operation ID.'))
        success=named(args,'successType') or positional(args,3); status=named(args,'successStatusCode')
        aliases={m.group('alias'): m.group('namespace') for m in ALIAS_RE.finditer(text)}
        if success:
            for alias, namespace in aliases.items(): success=re.sub(rf'\b{alias}\.', namespace + '.', success)
        status_token=(status or DEFAULT_STATUS[method]).strip()
        profile=named(args,'profile')
        if not success and status_token not in NO_BODY_STATUSES and profile and any(x in profile for x in ('MultipartUpload','LongRunningCommand')):
            out.append(V(p,lineno,'SuccessMetadataMissing',method,'Provide successType: typeof(ResponseContract) or explicit no-body successStatusCode such as StatusCodes.Status204NoContent.'))
        if status is None and method in {'MapModulePost','MapModulePut','MapModulePatch'} and profile and 'LongRunningCommand' in profile:
            out.append(V(p,lineno,'SuccessStatusCodeImplicit',method,'Provide explicit successStatusCode when the helper default is not semantically correct.'))
        if profile and ('BinaryDownload' in profile or 'Streaming' in profile) and not named(args,'contentType'):
            out.append(V(p,lineno,'ProfileContentTypeMissing',method,'BinaryDownload and Streaming endpoints must declare contentType.'))
        if profile and 'AnonymousWebhook' in profile and not named(args,'anonymousJustification'):
            out.append(V(p,lineno,'AnonymousJustificationMissing',method,'Anonymous endpoints must provide anonymousJustification.'))
        ok,reason=schema_ok(type_token(success))
        if not ok: out.append(V(p,lineno,'SchemaOwnershipViolation',type_token(success) or method,reason))
        handler=named(args,'handler') or positional(args,1)
        for rt in scan_handler_request_types(text, handler):
            for alias_match in ALIAS_RE.finditer(text): rt=re.sub(rf'\b{alias_match.group("alias")}\.', alias_match.group('namespace') + '.', rt)
            ok,reason=schema_ok(rt)
            if not ok: out.append(V(p,lineno,'RequestSchemaOwnershipViolation',rt,reason))
    for m in re.finditer(r'ExcludeFromDescription\s*\(', text):
        if 'ExcludeFromOpenApiWithJustification' not in text[max(0,m.start()-200):m.start()+200]:
            out.append(V(p,line(text,m.start()),'OpenApiExclusionJustificationMissing','ExcludeFromDescription','Use ExcludeFromOpenApiWithJustification with typed metadata; raw string metadata is not accepted.'))
    for m in re.finditer(r'OpenApiExclusionJustification:', text):
        out.append(V(p,line(text,m.start()),'RawOpenApiExclusionMetadata','OpenApiExclusionJustification','Use ExcludeFromOpenApiWithJustification; raw string metadata is not accepted.'))
    return out
violations=[]
for p in files(): violations.extend(scan(p))
print('OpenAPI endpoint coverage validation')
print('Violations found:', len(violations))
for v in violations: print(f'{v.path}:{v.line}: kind={v.kind}; endpoint={v.expr}; remediation={v.fix}')
sys.exit(1 if violations else 0)
