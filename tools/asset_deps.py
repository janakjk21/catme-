# Asset usage report: follows GUID references from the build scenes, every Resources folder and StreamingAssets,
# then prints total vs used MB per top-level Assets folder and writes the used file list to tools/asset_used.txt.
# Run from the project root: python3 tools/asset_deps.py

import os,re,collections
root='Assets'
guid2path={}
for d,_,fs in os.walk(root):
    for f in fs:
        if f.endswith('.meta'):
            p=os.path.join(d,f)
            try:
                with open(p,errors='ignore') as fh:
                    for line in fh:
                        if line.startswith('guid:'): guid2path[line.split()[1]]=p[:-5]; break
            except: pass
textext=('.unity','.prefab','.mat','.asset','.controller','.anim','.overrideController','.playable','.shadergraph','.shadersubgraph','.mixer','.lighting','.terrainlayer','.spriteatlas','.physicMaterial','.renderTexture','.cubemap','.flare','.brush','.signal','.preset','.vfx')
gre=re.compile(r'guid: ([0-9a-f]{32})')
seeds=['Assets/CatMe/Scenes/GardenCompanion.unity','Assets/CatMe/Scenes/Places/Place_HomeGarden_r2.unity']
for d,_,fs in os.walk(root):
    if '/Resources' in d or d.endswith('Resources') or d.startswith('Assets/StreamingAssets'):
        for f in fs:
            if not f.endswith('.meta'): seeds.append(os.path.join(d,f))
seen=set(); stack=list(seeds)
while stack:
    p=stack.pop()
    if p in seen or not os.path.exists(p): continue
    seen.add(p)
    if p.endswith(textext):
        try: txt=open(p,errors='ignore').read()
        except: continue
        for g in gre.findall(txt):
            q=guid2path.get(g)
            if q and q not in seen: stack.append(q)
    if os.path.exists(p+'.meta'):
        try:
            for g in gre.findall(open(p+'.meta',errors='ignore').read()):
                q=guid2path.get(g)
                if q and q not in seen: stack.append(q)
        except: pass
def top(p):
    parts=p.split('/'); return '/'.join(parts[:2])
used=collections.Counter(); total=collections.Counter()
for d,_,fs in os.walk(root):
    for f in fs:
        if f.endswith('.meta'): continue
        p=os.path.join(d,f); s=os.path.getsize(p); total[top(p)]+=s
        if p in seen: used[top(p)]+=s
print(f"{'folder':55} {'total MB':>9} {'used MB':>8}")
for k,v in total.most_common(40):
    print(f"{k:55} {v/1e6:9.1f} {used[k]/1e6:8.1f}")
print('TOTAL',sum(total.values())/1e6,'USED',sum(used.values())/1e6)
open('tools/asset_used.txt','w').write('\n'.join(sorted(seen)))
