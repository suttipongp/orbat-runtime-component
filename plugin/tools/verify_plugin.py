from __future__ import annotations
import hashlib,json,re,sys
from pathlib import Path
from jsonschema import Draft202012Validator,FormatChecker
from referencing import Registry,Resource
REPO=Path(__file__).resolve().parents[2];DIST=REPO/'dist'/'c4isr-orbat-plugin';CONTRACTS=DIST/'contracts'/'v1'
def load(p):return json.loads(p.read_text(encoding='utf-8'))
def fail(msg):print('FAIL '+msg,file=sys.stderr);raise SystemExit(1)
def confined(root,rel):
    p=(root/rel).resolve()
    if root.resolve()!=p and root.resolve() not in p.parents:fail('path escapes package: '+rel)
    return p
def main():
    required=['manifest.json','contracts/v1','library/definitions','web/orbat-renderer.js','fixtures/render-cases','checksums.json','README.md']
    for rel in required:
        if not confined(DIST,rel).exists():fail('missing '+rel)
    registry=Registry()
    for p in CONTRACTS.glob('*.schema.json'):
        s=load(p);registry=registry.with_resource(s['$id'],Resource.from_contents(s))
    def validator(name):
        s=load(CONTRACTS/name);return Draft202012Validator(s,registry=registry,format_checker=FormatChecker())
    manifest=load(DIST/'manifest.json');errs=list(validator('extension-manifest.schema.json').iter_errors(manifest))
    if errs:fail('manifest schema: '+errs[0].message)
    entry=manifest.get('protocol',{}).get('entryPoint','');ep=confined(DIST,entry)
    if not ep.is_file():fail('entrypoint missing')
    renderer=ep.read_text(encoding='utf-8')
    for api in ('renderSymbol','validateDefinition','getRendererCapabilities'):
        if not re.search(r'export\s+function\s+'+api+r'\b',renderer):fail('renderer missing API '+api)
    if re.search(r'\beval\s*\(|new\s+Function\b|System\.Drawing|Windows\.Forms',renderer):fail('renderer contains forbidden runtime feature')
    dval=validator('symbol-definition.schema.json');seen=set();definitions=[]
    for p in sorted((DIST/'library'/'definitions').glob('*.json')):
        d=load(p);errors=list(dval.iter_errors(d))
        if errors:fail(p.name+' schema: '+errors[0].message)
        key=(d['libraryId'],d['libraryRevision'],d['definitionId'],d['definitionRevision'])
        if key in seen:fail('duplicate definition '+str(key))
        seen.add(key);definitions.append(d)
        expected=d['role'] in {'main-function','composite'}
        if d['placeable']!=expected:fail('invalid placeable role '+d['definitionId'])
    if not definitions:fail('no definitions')
    ival=validator('symbol-instance.schema.json')
    for case in load(DIST/'fixtures'/'render-cases'/'cases.json')['cases']:
        errors=list(ival.iter_errors(case['instance']))
        if errors:fail('fixture '+case['id']+' schema: '+errors[0].message)
    for p in sorted((DIST/'library'/'instances').glob('*.json')):
        errors=list(ival.iter_errors(load(p)))
        if errors:fail(p.name+' migrated instance schema: '+errors[0].message)
    templates=manifest.get('contributions',{}).get('symbolTemplates',[])
    for t in templates:
        r=t['definition'];key=(r['libraryId'],r['libraryRevision'],r['definitionId'],r['definitionRevision'])
        if key not in seen:fail('template references missing definition '+r['definitionId'])
        d=next(x for x in definitions if (x['libraryId'],x['libraryRevision'],x['definitionId'],x['definitionRevision'])==key)
        if not d['placeable']:fail('component exposed as template '+d['definitionId'])
    golden=load(DIST/'fixtures'/'render-cases'/'golden-hashes.json')
    for case_id,expected in golden['hashes'].items():
        svg_path=confined(DIST,'fixtures/render-cases/svg/'+case_id+'.svg')
        if not svg_path.is_file():fail('missing golden SVG '+case_id)
        svg=svg_path.read_text(encoding='utf-8').rstrip('\n')
        if hashlib.sha256(svg.encode()).hexdigest()!=expected:fail('golden SVG hash mismatch '+case_id)
    checks=load(DIST/'checksums.json')
    if checks.get('algorithm')!='sha256':fail('unsupported checksum algorithm')
    actual={}
    for p in sorted(DIST.rglob('*')):
        if p.is_file() and p.name!='checksums.json':actual[p.relative_to(DIST).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
    if checks.get('files')!=actual:fail('checksum mismatch')
    report=load(DIST/'library'/'migration-report.json')
    if report.get('sourceMutated') is not False:fail('legacy adapter did not attest read-only behavior')
    print(f'VERIFIED manifest=1 definitions={len(definitions)} templates={len(templates)} checksums={len(actual)}')
if __name__=='__main__':main()
