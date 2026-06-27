#!/usr/bin/env python3
"""Validate ADR-0003 layer dependency boundaries.

This validator is intentionally fail-fast: configuration drift, temporary
exceptions, report-only mode, omitted migrated projects, and boundary violations
all return a non-zero exit code.
"""
from __future__ import annotations

import argparse, json, re, sys, xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

MODES={"FailFast","ReportOnly"}
FRONTEND_SAFE={"Interface","Client","Contracts"}
LAYERS={"Interface","Client","Contracts","Api","Application","Domain"}
BACKEND={"Application","Domain","Infrastructure","Api"}
SERVER_ONLY=("Entities.Server",)
SOURCE_EXT={".cs",".razor"}

@dataclass(frozen=True)
class ProjectScope:
    name:str; layer:str; project:Path; root:Path; module:str|None=None

@dataclass(frozen=True)
class Violation:
    scope:str; mode:str; kind:str; path:Path; line:int; forbidden:str; reason:str

def repo()->Path: return Path(__file__).resolve().parents[2]
def rel(root:Path,p:Path)->str:
    try: return p.resolve().relative_to(root).as_posix()
    except ValueError: return p.as_posix()
def line_no(text:str,offset:int)->int: return text.count('\n',0,max(offset,0))+1

def load_config(root:Path,path:Path)->dict:
    cfg=json.loads(path.read_text())
    if cfg.get('defaultMode')!='FailFast': raise ValueError('defaultMode must be FailFast')
    for name,settings in cfg.get('modules',{}).items():
        if settings.get('mode',cfg['defaultMode'])!='FailFast': raise ValueError(f'module {name} must be FailFast')
        if settings.get('temporaryExceptions'): raise ValueError(f'module {name} temporaryExceptions must be empty')
    for name,settings in cfg.get('globalInterfaces',{}).items():
        if settings.get('mode',cfg['defaultMode'])!='FailFast': raise ValueError(f'global Interface group {name} must be FailFast')
        if settings.get('temporaryExceptions'): raise ValueError(f'global Interface group {name} temporaryExceptions must be empty')
    if cfg.get('temporaryExceptions'): raise ValueError('top-level temporaryExceptions must be empty')
    return cfg

def discover(root:Path)->list[ProjectScope]:
    scopes=[]
    for p in sorted(root.glob('src/Modules/*/*/*.csproj')):
        parts=p.relative_to(root).parts
        if len(parts)>=5 and parts[3] in LAYERS:
            scopes.append(ProjectScope(f'{parts[2]}.{parts[3]}',parts[3],p,p.parent,parts[2]))
    for p in sorted(root.glob('src/Interfaces/*/*.csproj')):
        name=p.parent.name
        scopes.append(ProjectScope(name,'GlobalInterface',p,p.parent,None))
    return scopes

def ensure_coverage(root:Path,cfg:dict,scopes:list[ProjectScope])->list[Violation]:
    v=[]; discovered_modules=sorted({s.module for s in scopes if s.module and s.layer=='Interface'})
    configured=sorted(cfg.get('modules',{}))
    for m in set(discovered_modules)-set(configured):
        v.append(Violation(m,'FailFast','ConfigurationCoverage',Path('tools/architecture/interface_boundary_config.json'),1,m,'Migrated module Interface is omitted from validation config.'))
    for m in set(configured)-set(discovered_modules):
        v.append(Violation(m,'FailFast','ConfigurationCoverage',Path('tools/architecture/interface_boundary_config.json'),1,m,'Configured module Interface project was not discovered.'))
    discovered_globals=sorted(s.name for s in scopes if s.layer=='GlobalInterface')
    configured_globals=sorted(cfg.get('globalInterfaces',{}))
    for g in set(discovered_globals)-set(configured_globals):
        v.append(Violation(g,'FailFast','ConfigurationCoverage',Path('tools/architecture/interface_boundary_config.json'),1,g,'Global frontend Interface group is omitted from validation config.'))
    for g in set(configured_globals)-set(discovered_globals):
        v.append(Violation(g,'FailFast','ConfigurationCoverage',Path('tools/architecture/interface_boundary_config.json'),1,g,'Configured global Interface group was not discovered.'))
    return v

def project_ref_path(project:Path,inc:str)->Path: return (project.parent/inc.replace('\\','/')).resolve()
def classify_ref(root:Path,scope:ProjectScope,target:Path,cfg:dict)->tuple[str,str]|None:
    r=rel(root,target)
    # Server-only library is never frontend safe.
    if 'Libraries/Entities/Entities.Server' in r and scope.layer in {'Interface','Client','Contracts','GlobalInterface'}:
        return 'Entities.Server','This layer must not reference server-only Entities.Server.'
    
    # Validate against frontend-safe allowlist
    if scope.layer in {'Interface','Client','Contracts','GlobalInterface'}:
        if not any(r.endswith(a['project']) for a in cfg.get('frontendSafeDependencyAllowlist', [])):
            m=re.match(r'src/Modules/([^/]+)/([^/]+)/[^/]+\.csproj$',r)
            if m:
                tm,tl=m.groups()
                if scope.layer in {'Interface','GlobalInterface'}:
                    if tl in BACKEND or tl=='Infrastructure': return f'{tm}.{tl}','Frontend Interface projects must not reference backend projects.'
                    if tl=='Client' and scope.module and tm!=scope.module:
                        if not any(a['sourceModule']==scope.module and a['targetProject']==r for a in cfg.get('crossModuleFrontendSafeAllowlist', [])):
                            return f'{tm}.Client','Interface projects may only reference same-module Client projects or explicitly allowed cross-module projects.'
                if scope.layer=='Client' and tl!='Contracts': return f'{tm}.{tl}','Client projects may reference only Contracts plus approved frontend-safe base libraries.'
                if scope.layer=='Contracts' and tl!='Contracts': return f'{tm}.{tl}','Contracts projects may not reference module implementation layers.'
    
    if scope.layer=='Api' and (tl in {'Interface','Client','Infrastructure'} or tl=='Domain'):
        return f'{tm}.{tl}','Api projects must adapt Contracts to Application without Interface, Client, Infrastructure, or direct Domain dependencies.'
    return None
    r=rel(root,target)
    # Server-only library is never frontend safe.
    if 'Libraries/Entities/Entities.Server' in r and scope.layer in {'Interface','Client','Contracts','GlobalInterface'}:
        return 'Entities.Server','This layer must not reference server-only Entities.Server.'
    m=re.match(r'src/Modules/([^/]+)/([^/]+)/[^/]+\.csproj$',r)
    if m:
        tm,tl=m.groups()
        if scope.layer in {'Interface','GlobalInterface'}:
            if tl in BACKEND or tl=='Infrastructure': return f'{tm}.{tl}','Frontend Interface projects must not reference backend projects.'
            if tl=='Client' and scope.module and tm!=scope.module: return f'{tm}.Client','Interface projects may only reference same-module Client projects.'
        if scope.layer=='Client' and tl!='Contracts': return f'{tm}.{tl}','Client projects may reference only Contracts plus approved frontend-safe base libraries.'
        if scope.layer=='Contracts' and tl!='Contracts': return f'{tm}.{tl}','Contracts projects may not reference module implementation layers.'
        if scope.layer=='Api' and (tl in {'Interface','Client','Infrastructure'} or tl=='Domain'):
            return f'{tm}.{tl}','Api projects must adapt Contracts to Application without Interface, Client, Infrastructure, or direct Domain dependencies.'
    return None

def scan_project(root:Path,scope:ProjectScope,cfg:dict)->list[Violation]:
    text=scope.project.read_text(); v=[]
    try: xml=ET.fromstring(text)
    except ET.ParseError as e:
        return [Violation(scope.name,'FailFast','ProjectXml',scope.project.relative_to(root),getattr(e,'position',(1,))[0],scope.name,'Project XML could not be parsed.')]
    for item in xml.iter():
        if item.tag.split('}')[-1]!='ProjectReference': continue
        inc=item.attrib.get('Include');
        if not inc: continue
        c=classify_ref(root,scope,project_ref_path(scope.project,inc),cfg)
        if c:
            f,reason=c; v.append(Violation(scope.name,'FailFast','ProjectReference',scope.project.relative_to(root),line_no(text,text.find(inc)),f,reason))
    return v

def forbidden_namespaces(scopes:list[ProjectScope])->list[tuple[re.Pattern[str],str,str]]:
    mods=sorted({s.module for s in scopes if s.module})
    pats=[]
    for mod in mods:
        for layer in ['Application','Domain','Infrastructure','Api']:
            for token in [fr'Vitarish\.{mod}\.{layer}',fr'Vitarish\.Modules\.{mod}\.{layer}',fr'{mod}\.{layer}']:
                pats.append((re.compile(fr'(?<![A-Za-z0-9_]){token}(?:\b|\.)'),f'{mod}.{layer}','Frontend-safe source must not use backend namespaces.'))
    pats.append((re.compile(r'(?<![A-Za-z0-9_])Vitarish\.Entities\.Server(?:\b|\.)'),'Entities.Server','Frontend-safe source must not use server-only Entities namespaces.'))
    return pats

def scan_source(root:Path,scope:ProjectScope,pats)->list[Violation]:
    if scope.layer not in {'Interface','Client','Contracts','GlobalInterface'}: return []
    v=[]
    for p in sorted(x for ext in SOURCE_EXT for x in scope.root.rglob(f'*{ext}') if 'bin/' not in x.as_posix() and 'obj/' not in x.as_posix()):
        text=p.read_text(errors='ignore'); seen=set()
        for pat,forb,reason in pats:
            for m in pat.finditer(text):
                ln=line_no(text,m.start()); key=(ln,forb)
                if key in seen: continue
                seen.add(key); v.append(Violation(scope.name,'FailFast','NamespaceUsage',p.relative_to(root),ln,forb,reason))
    return v

def collect(root:Path,cfg:dict)->list[Violation]:
    scopes=discover(root); pats=forbidden_namespaces(scopes); v=ensure_coverage(root,cfg,scopes)
    for s in scopes:
        v+=scan_project(root,s,cfg); v+=scan_source(root,s,pats)
    return v

def main()->int:
    ap=argparse.ArgumentParser(description='Validate ADR-0003 layer boundaries.'); ap.add_argument('--config',default='tools/architecture/interface_boundary_config.json'); args=ap.parse_args()
    root=repo()
    try: cfg=load_config(root,root/args.config)
    except Exception as e: print(f'Configuration error: {e}'); return 1
    v=collect(root,cfg)
    scopes=discover(root)
    print('ADR-0003 layer boundary validation'); print(f'Configuration: {args.config}')
    print('Module Interface groups: '+', '.join(sorted({s.module for s in scopes if s.layer=='Interface' and s.module})))
    print('Global Interface groups: '+', '.join(sorted(s.name for s in scopes if s.layer=='GlobalInterface')))
    print(f'Violations found: {len(v)}')
    for x in v:
        print(f'{x.mode}: {x.kind}: {x.path}:{x.line}: scope={x.scope}; forbidden={x.forbidden}; reason={x.reason}')
    return 1 if v else 0
if __name__=='__main__': sys.exit(main())
