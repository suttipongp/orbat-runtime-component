from __future__ import annotations
import hashlib, json, re
from pathlib import Path

CONTRACT_VERSION="1.0"
LIBRARY_ID="orbat.standard-symbol-library"
LIBRARY_REVISION=1
STROKE_REFERENCE_SIZE=256
KNOWN={"Version","Name","LibraryId","LibraryVersion","UnitType","UnitCategory","UnitMainFunction","EquipmentCategory","EquipmentFunction","Variant","SymbolRole","CompositionMode","Layout","Modifier1Type","Modifier2Type","LandUnitModifier1Type","LandUnitModifier2Type","MobilityType","EchelonType","Affiliation","PhysicalDomain","FrameShape","FrameStatus","OperatingState","Commands"}
ROLE_MAP={"MainFunction":"main-function","Modifier1":"modifier-1","Modifier2":"modifier-2","EchelonIndicator":"echelon-indicator","MobilityIndicator":"mobility-indicator","Composite":"composite"}

def read_json(path:Path):
    with path.open(encoding="utf-8-sig") as f:return json.load(f)
def write_json(path:Path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,indent=2,ensure_ascii=False,sort_keys=True)+"\n",encoding="utf-8")
def sha256(path:Path):return hashlib.sha256(path.read_bytes()).hexdigest()
def point(p):return {"x":float((p or {}).get("X",0)),"y":float((p or {}).get("Y",0))}
def slug(v):
    s=re.sub(r"[^a-z0-9]+","-",str(v or "").strip().lower()).strip("-")
    return s or "unspecified"
def infer_domain(raw,path):
    v=str(raw.get("PhysicalDomain") or "").lower()
    return "equipment" if v=="equipment" or path.name.lower().startswith("equipment.") else "land-unit"
def infer_role(raw,domain):
    role=ROLE_MAP.get(str(raw.get("SymbolRole") or ""))
    if role:return role
    return "composite" if domain=="land-unit" else "main-function"
def semantic_type(raw,domain,role):
    if domain=="land-unit":
        if role=="modifier-1":return raw.get("LandUnitModifier1Type") or raw.get("Modifier1Type")
        if role=="modifier-2":return raw.get("LandUnitModifier2Type") or raw.get("Modifier2Type")
        if role=="echelon-indicator":return raw.get("EchelonType")
        return raw.get("UnitMainFunction") or raw.get("UnitType") or raw.get("Name")
    if role=="modifier-1":return raw.get("Modifier1Type")
    if role=="modifier-2":return raw.get("Modifier2Type")
    if role=="mobility-indicator":return raw.get("MobilityType")
    return raw.get("EquipmentFunction") or raw.get("Name")
def legacy_effective_id(raw,domain,role):
    stored=str(raw.get("LibraryId") or "").strip().lower()
    if stored:return stored
    domain_name="Equipment" if domain=="equipment" else "LandUnit"
    role_name={"main-function":"MainFunction","modifier-1":"Modifier1","modifier-2":"Modifier2","echelon-indicator":"EchelonIndicator","mobility-indicator":"MobilityIndicator","composite":"Composite"}[role]
    value=str(semantic_type(raw,domain,role) or "").strip() or "unspecified"
    variant=str(raw.get("Variant") or "").strip() or "unspecified"
    operating=str(raw.get("OperatingState") or "Ground") if domain=="equipment" else "Ground"
    composition=str(raw.get("CompositionMode") or ("Composite" if role=="composite" else "Composable"))
    key="|".join([domain_name,role_name,value,variant,operating,composition]).lower()
    return hashlib.sha256(key.encode()).hexdigest()[:32]
def identity(raw,domain,role):return "orbat.definition."+slug(legacy_effective_id(raw,domain,role))
def style(command):
    return {"stroke":"#111111","strokeWidth":max(0,float(command.get("StrokeWidth") or 2)/STROKE_REFERENCE_SIZE),"fill":"#111111" if command.get("Filled") else None,"lineCap":"round","lineJoin":"round"}
def primitive(command,index):
    kind=str(command.get("Kind") or "").lower(); start=point(command.get("Start")); end=point(command.get("End")); base={"id":f"p{index:03d}","style":style(command)}
    if command.get("GroupId"):base["groupId"]=str(command["GroupId"])
    if abs(float(command.get("RotationDegrees") or 0))>.00001:base["rotationDegrees"]=float(command["RotationDegrees"])
    if kind in {"line","rectangle","ellipse","capsule","arc"}:base.update(type=kind,start=start,end=end)
    elif kind in {"circle","dot"}:
        r=max(0,float(command.get("Radius") or 0));base.update(type=kind,start={"x":start["x"]-r,"y":start["y"]-r},end={"x":start["x"]+r,"y":start["y"]+r})
        if kind=="dot":base["style"]["fill"]="#111111"
    elif kind in {"path","polyline"}:
        pts=[point(p) for p in command.get("Points") or []];base.update(type="path" if kind=="path" else "polyline",points=pts,closed=bool(command.get("Filled") or (len(pts)>2 and pts[0]==pts[-1])))
    elif kind=="bezier":base.update(type="bezier",start=start,control1=point(command.get("Control1")),control2=point(command.get("Control2")),end=end)
    elif kind=="sinewave":base.update(type="sine-wave",start=start,end=end,cycles=4,amplitude=abs(end["y"]-start["y"])/2)
    elif kind=="text":
        base={"id":base["id"],"type":"text","position":start,"text":str(command.get("Text") or ""),"fontFamily":"sans-serif","fontSize":max(.01,float(command.get("FontSize") or 12)/100),"fontWeight":"bold","textAnchor":"middle","style":{"fill":"#111111","stroke":None,"strokeWidth":0}}
        if abs(float(command.get("RotationDegrees") or 0))>.00001:base["rotationDegrees"]=float(command["RotationDegrees"])
    else:raise ValueError(f"unsupported command kind {command.get('Kind')}")
    return base
def convert_symbol(path:Path,root:Path):
    raw=read_json(path);domain=infer_domain(raw,path);role=infer_role(raw,domain);composition=str(raw.get("CompositionMode") or ("Composite" if role=="composite" else "Composable")).lower();placeable=role in {"main-function","composite"}
    classification={"category":str(raw.get("UnitCategory") or raw.get("EquipmentCategory") or ""),"function":str(semantic_type(raw,domain,role) or "Unspecified"),"variant":str(raw.get("Variant") or ""),"modifierType":str(raw.get("Modifier1Type") or raw.get("Modifier2Type") or raw.get("LandUnitModifier1Type") or raw.get("LandUnitModifier2Type") or ""),"operatingState":str(raw.get("OperatingState") or "Ground")}
    unknown={k:v for k,v in raw.items() if k not in KNOWN}
    legacy={"sourcePath":path.relative_to(root).as_posix(),"format":"orbatsymbol-json","version":int(raw.get("Version") or 1),"storedLibraryId":str(raw.get("LibraryId") or ""),"legacyEffectiveLibraryId":legacy_effective_id(raw,domain,role),"storedLibraryVersion":int(raw.get("LibraryVersion") or 0),"frameShape":str(raw.get("FrameShape") or ""),"frameStatus":str(raw.get("FrameStatus") or ""),"affiliation":str(raw.get("Affiliation") or "")}
    if unknown:legacy["unknownFields"]=unknown
    definition={"contractVersion":CONTRACT_VERSION,"libraryId":LIBRARY_ID,"libraryRevision":LIBRARY_REVISION,"definitionId":identity(raw,domain,role),"definitionRevision":max(1,int(raw.get("LibraryVersion") or raw.get("Version") or 1)),"name":str(raw.get("Name") or semantic_type(raw,domain,role) or "Unnamed"),"domain":domain,"role":role,"composition":composition if composition in {"composable","composite"} else "composable","placeable":placeable,"classification":classification,"scene":{"coordinateSpace":"normalized-cartesian","viewBox":{"x":0,"y":0,"width":1,"height":1},"primitives":[primitive(c,i) for i,c in enumerate(raw.get("Commands") or [])]},"extensions":{"legacy":legacy}}
    layout=raw.get("Layout")
    if isinstance(layout,dict):definition["extensions"]["legacy"]["layout"]=layout
    return definition,unknown
def ref(definition):return {"contractVersion":CONTRACT_VERSION,"libraryId":definition["libraryId"],"libraryRevision":definition["libraryRevision"],"definitionId":definition["definitionId"],"definitionRevision":definition["definitionRevision"]}
def enum_value(value,mapping,fallback):return mapping.get(str(value or "").lower(),fallback)
def convert_overlay(path:Path,root:Path,resolver:dict[str,dict]):
    raw=read_json(path);legacy_ids={"main":str(raw.get("LibraryId") or ""),"modifier1":str(raw.get("Modifier1LibraryId") or ""),"modifier2":str(raw.get("Modifier2LibraryId") or ""),"mobility":str(raw.get("MobilityLibraryId") or "")}
    resolved={key:resolver.get(value.lower()) for key,value in legacy_ids.items() if value}
    missing={key:value for key,value in legacy_ids.items() if value and key not in resolved}
    main=resolved.get("main")
    instance=None
    if main:
        components={"mainFunction":ref(main)}
        for old,new in (("modifier1","modifier1"),("modifier2","modifier2"),("mobility","mobility")):
            if resolved.get(old):components[new]=ref(resolved[old])
        instance={"contractVersion":CONTRACT_VERSION,"id":str(raw.get("InstanceId") or path.stem),"template":ref(main),"components":components,"domain":"equipment" if str(raw.get("Domain") or "").lower()=="equipment" else "land-unit","affiliation":enum_value(raw.get("Affiliation"),{"friendly":"friend","friend":"friend","hostile":"hostile","neutral":"neutral","unknown":"unknown","suspect":"suspect","civilian":"civilian"},"unknown"),"status":{"frame":enum_value(raw.get("FrameStatus"),{"present":"present","planned":"planned-anticipated","anticipated":"planned-anticipated","plannedanticipated":"planned-anticipated"},"present"),"operatingState":str(raw.get("OperatingState") or "Ground")},"classification":{"function":str(raw.get("Function") or main.get("classification",{}).get("function") or "Unspecified"),"variant":str(raw.get("Variant") or "")},"amplifierValues":raw.get("Amplifiers") or {}}
    known={"InstanceId","LibraryId","LibraryVersion","Modifier1LibraryId","Modifier1LibraryVersion","Modifier2LibraryId","Modifier2LibraryVersion","MobilityLibraryId","MobilityLibraryVersion","Domain","Affiliation","FrameStatus","OperatingState","Function","Variant","X","Y","Width","Height","RotationDegrees","Amplifiers"}
    placement={"instanceId":str(raw.get("InstanceId") or path.stem),"coordinateSpace":"legacy-local","x":raw.get("X"),"y":raw.get("Y"),"width":raw.get("Width"),"height":raw.get("Height"),"rotationDegrees":raw.get("RotationDegrees")}
    migration={"sourcePath":path.relative_to(root).as_posix(),"instanceId":placement["instanceId"],"legacyDefinitionIds":legacy_ids,"resolvedDefinitionIds":{key:value["definitionId"] for key,value in resolved.items()},"unresolvedDefinitionIds":missing,"unknownFields":{k:v for k,v in raw.items() if k not in known}}
    return instance,placement,migration
