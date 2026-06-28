#!/usr/bin/env python3
"""Validate ADR-0004 protected internal module API boundaries."""
from __future__ import annotations
import re, sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BASE_ENDPOINT_FILE = Path('src/Modules/Base/Core/Endpoints/ProtectedModuleEndpointRouteBuilderExtensions.cs')
APPROVED_BASE_CLIENT_ABSTRACTIONS = {
 'src/Modules/Base/Client/AuthenticatedModuleApiClient.cs',
 'src/Modules/Base/Client/DependencyInjection/InternalModuleClientRegistrationExtensions.cs',
}
EXTERNAL_CLIENT_EXCLUSIONS = {
 'src/Modules/Commerce/Infrastructure/Clients/MedusaAdminClient.cs',
 'src/Modules/Commerce/Infrastructure/Clients/MedusaStorefrontClient.cs',
 'src/Modules/Medical/Infrastructure/Providers/Prescriptions/Salamat/SalamatInsuranceAdapter.cs',
 'src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminInsuranceAdapter.cs',
 'src/Modules/Medical/Infrastructure/Providers/Prescriptions/Tamin/TaminCredentialSessionStore.cs',
 'src/Modules/Social/Infrastructure/Adapters/TelegramSocialProviderAdapter.cs',
 'src/Modules/Social/Infrastructure/Adapters/InstagramSocialProviderAdapter.cs',
 'src/Modules/Intelligence/Infrastructure/Services/Ollama/OllamaTranscriber.cs',
}
ROUTE_METHODS = {'MapGet','MapPost','MapPut','MapDelete','MapPatch'}

@dataclass
class V:
    path: Path; line: int; module: str; kind: str; forbidden: str; reason: str

def line(text,o): return text.count('\n',0,o)+1

def module_for(p:Path):
    parts=p.parts
    return parts[2] if len(parts)>2 and parts[0]=='src' and parts[1]=='Modules' else 'Unknown'

def cs_files(pattern): return [p.relative_to(ROOT) for p in ROOT.glob(pattern) if p.is_file()]

def unique(paths):
    return sorted(set(paths), key=lambda x: x.as_posix())

def scan_endpoint(p:Path):
    if p==BASE_ENDPOINT_FILE: return []
    text=(ROOT/p).read_text(errors='ignore')
    vs=[]; protected=set(); anonymous=set()
    for idx, raw_line in enumerate(text.splitlines(), start=1):
        m = re.search(r'(?:(?:RouteGroupBuilder|var)\s+)?(\w+)\s*=.*\.Map(Protected|Anonymous)ModuleGroup\((.*)\)', raw_line)
        if not m:
            continue
        var, kind, args = m.group(1), m.group(2), m.group(3)
        if kind == 'Protected':
            protected.add(var)
        else:
            strs = re.findall(r'"([^"]*)"', args)
            if strs and strs[-1].strip():
                anonymous.add(var)
            else:
                vs.append(V(p, idx, module_for(p), 'AnonymousGroupJustification', 'MapAnonymousModuleGroup', 'Anonymous module endpoints require a non-empty justification.'))
    for m in re.finditer(r'\b(\w+)\.MapGroup\s*\(', text):
        vs.append(V(p,line(text,m.start()),module_for(p),'DirectMapGroup','MapGroup','Internal module endpoints must use MapProtectedModuleGroup or justified MapAnonymousModuleGroup.'))
    for m in re.finditer(r'\b(\w+)\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\s*\(', text):
        recv,method=m.group(1),m.group(2)
        endpoint_fragment = '/Api/Endpoints/' in p.as_posix() and recv == 'endpoints'
        if recv not in protected and recv not in anonymous and not endpoint_fragment:
            vs.append(V(p,line(text,m.start()),module_for(p),'DirectEndpointMapping',method,'Endpoint mappings must be on an approved protected or justified anonymous route group.'))
    for m in re.finditer(r'\.AllowAnonymous\s*\(', text):
        vs.append(V(p,line(text,m.start()),module_for(p),'AllowAnonymous','AllowAnonymous','Internal module APIs must use MapAnonymousModuleGroup for anonymous exceptions.'))
    return vs

def scan_client(p:Path):
    if p.as_posix() in EXTERNAL_CLIENT_EXCLUSIONS or p.as_posix() in APPROVED_BASE_CLIENT_ABSTRACTIONS: return []
    text=(ROOT/p).read_text(errors='ignore')
    if 'HttpClient' in text and 'AuthenticatedModuleApiClient' not in text:
        i=text.find('HttpClient')
        return [V(p,line(text,i),module_for(p),'RawHttpClient','HttpClient','Internal module clients must use AuthenticatedModuleApiClient or approved authenticated transport.')]
    return []

def scan_di(p:Path):
    text=(ROOT/p).read_text(errors='ignore')
    vs=[]
    for m in re.finditer(r'\.(AddHttpClient|AddScoped|AddTransient|AddSingleton)\s*<([^>]*Client[^>]*)>', text):
        vs.append(V(p,line(text,m.start()),module_for(p),'DirectClientRegistration',m.group(1),'Internal module clients must be registered through AddInternalModuleApiClient<TClientContract,TClientImplementation>.'))
    return vs

violations=[]
for p in unique(cs_files('src/Modules/*/Api/**/*.cs')+cs_files('src/Modules/*/Api/*.cs')+cs_files('src/Modules/*/Module/**/*.cs')):
    violations += scan_endpoint(p)
for p in unique(cs_files('src/Modules/*/Client/**/*.cs')+cs_files('src/Modules/*/Client/*.cs')):
    violations += scan_client(p)
for p in unique(cs_files('src/Modules/*/Interface/**/DependencyInjection/**/*.cs')+cs_files('src/Modules/*/Interface/DependencyInjection/*.cs')):
    violations += scan_di(p)
print('ADR-0004 Module API boundary validation')
print('Approved endpoint abstraction:', BASE_ENDPOINT_FILE)
print('External provider client exclusions:', len(EXTERNAL_CLIENT_EXCLUSIONS))
print('Violations found:', len(violations))
for v in violations:
    print(f'{v.path}:{v.line}: module={v.module}; kind={v.kind}; forbidden={v.forbidden}; reason={v.reason}')
sys.exit(1 if violations else 0)
