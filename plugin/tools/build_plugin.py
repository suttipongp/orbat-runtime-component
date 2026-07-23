from __future__ import annotations
import argparse, hashlib, json, shutil, subprocess, sys
from pathlib import Path
from jsonschema import Draft202012Validator, FormatChecker
from referencing import Registry, Resource
from pluginlib import *

REPO=Path(__file__).resolve().parents[2]
PLUGIN=REPO/'plugin'; DIST=REPO/'dist'/'c4isr-orbat-plugin'; CONTRACTS=REPO/'contracts'/'v1'; LEGACY=REPO/'symbol-library'

def tree_hash(paths):
    h=hashlib.sha256()
    for p in sorted(paths,key=lambda x:x.as_posix().lower()):h.update(p.relative_to(REPO).as_posix().encode());h.update(p.read_bytes())
    return h.hexdigest()
def registry():
    r=Registry()
    for p in sorted(CONTRACTS.glob('*.schema.json')):
        s=read_json(p);r=r.with_resource(s['$id'],Resource.from_contents(s))
    return r
def validate(schema_name,value,where,reg):
    schema=read_json(CONTRACTS/schema_name);errors=sorted(Draft202012Validator(schema,registry=reg,format_checker=FormatChecker()).iter_errors(value),key=lambda e:list(e.path))
    if errors:raise ValueError(where+': '+'; '.join(('/'.join(map(str,e.path)) or '<root>')+' '+e.message for e in errors[:10]))
def clean_dist():
    target=DIST.resolve();root=(REPO/'dist').resolve()
    if root not in target.parents:raise RuntimeError('refusing unsafe output deletion')
    if DIST.exists():shutil.rmtree(DIST)
    DIST.mkdir(parents=True)
def manifest(definitions):
    templates=[]
    latest={}
    for d in definitions:
        if d['placeable']:
            key=(d['libraryId'],d['libraryRevision'],d['definitionId']);latest[key]=max(d,latest.get(key,d),key=lambda x:x['definitionRevision'])
    for d in sorted(latest.values(),key=lambda x:(x['domain'],x['classification']['function'].lower(),x['name'].lower(),x['definitionId'])):
        templates.append({'id':'template.'+d['definitionId'],'title':d['name'],'requiredPermission':'orbat.read','standard':'MIL-STD-2525','symbolSet':d['domain'],'unitType':d['classification']['function'],'defaultAffiliation':'friend','description':f"{d['domain']} {d['name']}",'definition':ref(d)})
    return {'contractVersion':'1.0','id':'c4isr.orbat-static-plugin','name':'ORBAT Runtime Static Plugin','version':'1.0.0','publisher':'Sutti','description':'Deterministic platform-neutral ORBAT symbol definitions and SVG renderer.','enabled':True,'requiredPermissions':['orbat.read'],'protocol':{'type':'static-assets','version':'1','entryPoint':'web/orbat-renderer.js'},'contributions':{'importConnectors':[{'id':'orbat.import.legacy-json','title':'Legacy ORBAT JSON (read-only adapter)','requiredPermission':'orbat.read','accepts':['.orbatsymbol.json','.orbatoverlay.json'],'target':'migration-preview'}],'symbolRenderers':[{'id':'renderer.orbat.svg.v1','title':'ORBAT deterministic SVG renderer','requiredPermission':'orbat.read','standard':'MIL-STD-2525','version':'1.0.0','renderer':'web/orbat-renderer.js','supportedAffiliations':['friend','hostile','neutral','unknown','suspect','civilian'],'supportedEchelons':['Team','Squad','Section','Platoon','Company','Battalion','Regiment','Brigade','Division','Corps','Army']}],'symbolTemplates':templates}}
def choose(defs,domain,role):return next((d for d in defs if d['domain']==domain and d['role']==role),None)
def fixtures(defs):
    lm=choose(defs,'land-unit','composite') or choose(defs,'land-unit','main-function');em=choose(defs,'equipment','main-function') or choose(defs,'equipment','composite');m1=choose(defs,'equipment','modifier-1') or choose(defs,'land-unit','modifier-1');m2=choose(defs,'equipment','modifier-2') or choose(defs,'land-unit','modifier-2');mob=choose(defs,'equipment','mobility-indicator');ech=choose(defs,'land-unit','echelon-indicator')
    if not lm or not em:raise RuntimeError('library lacks placeable land-unit/equipment definitions')
    cases=[]
    configs=[('land-friendly-present','land-unit','friend','present',lm,[('modifier1',m1),('modifier2',m2),('echelon',ech)]),('land-hostile-planned','land-unit','hostile','planned-anticipated',lm,[('modifier1',m1)]),('land-neutral-present','land-unit','neutral','present',lm,[]),('land-unknown-present','land-unit','unknown','present',lm,[('echelon',ech)]),('equipment-friendly-composed','equipment','friend','present',em,[('modifier1',m1),('modifier2',m2),('mobility',mob)]),('equipment-hostile-present','equipment','hostile','present',em,[('mobility',mob)]),('equipment-neutral-planned','equipment','neutral','planned-anticipated',em,[]),('equipment-unknown-present','equipment','unknown','present',em,[('modifier1',m1)])]
    for name,domain,aff,status,main,parts in configs:
        components={'mainFunction':ref(main)}
        for key,d in parts:
            if d:components[key]=ref(d)
        instance={'contractVersion':'1.0','id':'fixture.'+name,'template':ref(main),'components':components,'domain':domain,'affiliation':aff,'status':{'frame':status,'operatingState':'Ground'},'amplifierValues':{}}
        for size in (32,48,96,256):cases.append({'id':f'{name}-{size}','size':size,'instance':instance})
    return {'contractVersion':'1.0','cases':cases}
def checksum_file():
    values={}
    for p in sorted(DIST.rglob('*')):
        if p.is_file() and p.name!='checksums.json':values[p.relative_to(DIST).as_posix()]=sha256(p)
    write_json(DIST/'checksums.json',{'algorithm':'sha256','files':values})
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--update-golden',action='store_true');args=ap.parse_args()
    legacy_files=sorted(LEGACY.glob('*.orbatsymbol.json'));overlay_files=sorted(LEGACY.glob('*.orbatoverlay.json'));before=tree_hash(legacy_files+overlay_files);clean_dist();reg=registry();defs=[];warnings=[];keys={}
    outdefs=DIST/'library'/'definitions'
    for p in legacy_files:
        d,unknown=convert_symbol(p,REPO);validate('symbol-definition.schema.json',d,p.name,reg);key=(d['definitionId'],d['definitionRevision'])
        body=json.dumps(d,sort_keys=True,separators=(',',':'))
        if key in keys and keys[key]!=body:
            original=d['definitionId'];fingerprint=hashlib.sha256(json.dumps({'classification':d['classification'],'scene':d['scene']},sort_keys=True,separators=(',',':')).encode()).hexdigest()[:12]
            d['definitionId']=original+'.'+fingerprint;key=(d['definitionId'],d['definitionRevision']);body=json.dumps(d,sort_keys=True,separators=(',',':'))
            warnings.append({'sourcePath':p.relative_to(REPO).as_posix(),'code':'IDENTITY_COLLISION_DISAMBIGUATED','originalDefinitionId':original,'definitionId':d['definitionId']})
            if key in keys and keys[key]!=body:raise RuntimeError(f'content fingerprint collision: {key}')
        keys[key]=body;defs.append(d);write_json(outdefs/f"{d['definitionId']}.r{d['definitionRevision']}.json",d)
        if unknown:warnings.append({'sourcePath':p.relative_to(REPO).as_posix(),'code':'UNKNOWN_FIELDS_PRESERVED','fields':sorted(unknown)})
    resolver={}
    for d in defs:
        legacy_id=d['extensions']['legacy']['legacyEffectiveLibraryId'].lower()
        if legacy_id in resolver and resolver[legacy_id]['definitionId']!=d['definitionId']:
            warnings.append({'code':'AMBIGUOUS_LEGACY_ID','legacyId':legacy_id,'selectedDefinitionId':resolver[legacy_id]['definitionId'],'alternateDefinitionId':d['definitionId']})
        else:resolver[legacy_id]=d
    overlays=[];migrated_instances=0
    for p in overlay_files:
        instance,placement,migration=convert_overlay(p,REPO,resolver);overlays.append(migration)
        if migration['unresolvedDefinitionIds']:warnings.append({'sourcePath':migration['sourcePath'],'code':'UNRESOLVED_OVERLAY_REFERENCE','references':migration['unresolvedDefinitionIds']})
        if instance:
            validate('symbol-instance.schema.json',instance,p.name,reg);write_json(DIST/'library'/'instances'/f"{slug(instance['id'])}.json",instance);write_json(DIST/'library'/'placements'/f"{slug(instance['id'])}.json",placement);migrated_instances+=1
        else:warnings.append({'sourcePath':migration['sourcePath'],'code':'UNRESOLVED_OVERLAY_TEMPLATE','legacyId':migration['legacyDefinitionIds']['main']})
    report={'contractVersion':'1.0','adapter':'legacy-json-read-only','sourceMutated':False,'symbolDefinitions':len(defs),'overlayInstances':len(overlays),'migratedSymbolInstances':migrated_instances,'warnings':warnings,'overlays':overlays}
    write_json(DIST/'library'/'migration-report.json',report)
    shutil.copytree(CONTRACTS,DIST/'contracts'/'v1')
    (DIST/'web').mkdir(parents=True,exist_ok=True)
    shutil.copy2(PLUGIN/'web'/'orbat-renderer.js',DIST/'web'/'orbat-renderer.js')
    mani=manifest(defs);validate('extension-manifest.schema.json',mani,'manifest.json',reg);write_json(DIST/'manifest.json',mani)
    case_data=fixtures(defs)
    for case in case_data['cases']:validate('symbol-instance.schema.json',case['instance'],'fixture '+case['id'],reg)
    write_json(PLUGIN/'fixtures'/'render-cases'/'cases.json',case_data);write_json(DIST/'fixtures'/'render-cases'/'cases.json',case_data)
    shutil.copy2(PLUGIN/'README.md',DIST/'README.md')
    runner=[str(PLUGIN/'tests'/'render-fixtures.mjs')]
    if args.update_golden:runner.append('--update')
    subprocess.run(['node',*runner],cwd=REPO,check=True)
    if (PLUGIN/'fixtures'/'render-cases'/'golden-hashes.json').exists():shutil.copy2(PLUGIN/'fixtures'/'render-cases'/'golden-hashes.json',DIST/'fixtures'/'render-cases'/'golden-hashes.json')
    after=tree_hash(legacy_files+overlay_files)
    if before!=after:raise RuntimeError('legacy source library was mutated')
    checksum_file();subprocess.run([sys.executable,str(PLUGIN/'tools'/'verify_plugin.py')],cwd=REPO,check=True)
    print(f'BUILT {DIST} definitions={len(defs)} fixtures={len(case_data["cases"])}')
if __name__=='__main__':main()
