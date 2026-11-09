#!/usr/bin/env python3
"""Phase 33B original code-authored art. Extends the reproducible Phase 33A pack.
Display stock is decorative, never a source of additional items or collisions.
"""
from pathlib import Path
import json,random,math
from PIL import Image,ImageDraw
import build_environment_33a as art
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'src/Ashen.Game/Assets'
# Number, retail identity, visual family, accent. Source names come from campaign.
STORES=[('travel',(81,125,145)),('stationery',(179,147,88)),('coffee',(136,91,61)),('deli',(137,136,78)),('coffee',(106,80,64)),('optical',(85,151,132)),('market',(128,153,124)),('optical',(89,114,131)),('bakery',(171,126,127)),('home',(143,128,103)),('garden',(96,138,83)),('records',(109,120,155)),('thrift',(146,122,99)),('bikes',(147,101,67)),('garden',(77,125,102)),('electronics',(84,132,150)),('beauty',(151,110,143)),('retro',(101,145,126))]
def rgb(c):return '#%02x%02x%02x'%tuple(c)
def mix(a,b,k):return tuple(round(x*(1-k)+y*k) for x,y in zip(a,b))
def sprite(name,family,accent,role):
 im=art.base(256,128);d=ImageDraw.Draw(im);a=rgb(accent);pale=rgb(mix(accent,(233,223,187),.65));dark='#293b3c'
 art.box(d,(8,16,251,121),(0,0,0,64),None, radius=7)
 art.box(d,(5,6,246,111),a,pale,3);art.box(d,(12,14,239,99),'#344543',dark,2)
 if role=='seating':
  art.box(d,(17,25,235,99),a,pale,3);art.box(d,(8,10,244,38),rgb(mix(accent,(30,40,35),.25)),pale,2)
  for x in (24,94,164):art.box(d,(x,44,x+62,89),rgb(mix(accent,(210,192,150),.2)),dark,1)
  d.line((18,105,236,105),fill=pale,width=2)
 elif role=='stock':
  for x in (20,93,166):
   art.box(d,(x,23,x+64,91),'#a18a62','#d2c195',2);d.rectangle((x+25,24,x+34,91),fill='#c4b484');art.box(d,(x+9,44,x+23,65),'#d8d0ad',dark,1)
  for x in range(28,222,35):d.line((x,98,x,107),fill=dark,width=2)
 elif family=='optical':
  for y in (27,63):
   for x in range(24,218,44):
    d.ellipse((x,y,x+15,y+16),outline=pale,width=3);d.ellipse((x+21,y,x+36,y+16),outline=pale,width=3);d.line((x+15,y+7,x+21,y+7),fill=pale,width=2)
  d.line((15,54,238,54),fill=accent,width=3)
  if role=='feature':
   d.line((183,17,196,87),fill='#acc6b8',width=2);d.line((199,17,178,69),fill='#698b85',width=1)
 elif family=='electronics':
  for i,x in enumerate((27,88,159)):
   art.box(d,(x,24,x+44,80),dark,'#aabdb1',2);art.box(d,(x+5,29,x+39,70),['#557d7e','#5b6e73','#496061'][i],width=1)
   d.ellipse((x+20,73,x+24,77),fill=pale)
  d.line((37,91,193,90,213,83,228,93),fill='#202f31',width=3)
 elif family=='records':
  for x in (20,92,164):
   art.box(d,(x,19,x+65,95),'#857154','#c0b48e',2)
   for i in range(5):art.box(d,(x+5+i*3,26+i*9,x+56+i,39+i*9),['#6f8c8a','#b4a17a','#836f7c'][i%3],dark,1,0)
   d.ellipse((x+20,46,x+47,73),fill='#233334',outline='#a29b78',width=1);d.ellipse((x+31,57,x+36,62),fill='#bf9d66')
 elif family=='bikes':
  for x in (24,140):
   d.ellipse((x,27,x+67,94),fill='#263537',outline='#b5c3b8',width=5);d.ellipse((x+9,36,x+58,85),outline='#617b77',width=2)
   for t in range(0,360,45):
    dx,dy=math.cos(math.radians(t))*28,math.sin(math.radians(t))*28;d.line((x+33,60,x+33+dx,60+dy),fill='#7b9690',width=1)
  d.line((59,59,98,23,173,59,59,59,112,87,98,23),fill=pale,width=4);d.line((173,59,190,20,217,21),fill='#b8c9bc',width=4);d.line((100,19,126,19),fill='#242f30',width=7)
 elif family=='garden':
  rng=random.Random(330+(role=='feature'))
  for x in (35,97,159,210):
   d.ellipse((x-19,26,x+18,92),fill='#95765a',outline='#c5ad7c',width=2)
   for i in range(10):
    xx,yy=x+rng.randint(-15,12),rng.randint(32,81);d.line((x,62,xx,yy),fill='#65834e',width=2);d.ellipse((xx-9,yy-4,xx+9,yy+4),fill=rng.choice(['#496d4c','#739059','#8a9d62']),outline='#314d3c')
 elif family in ('coffee','bakery','deli'):
  for x in (18,91,164):
   art.box(d,(x,20,x+66,91),'#8e9e8e','#d2d6bc',2)
   if family=='coffee':
    for k in range(3):
     d.ellipse((x+7+k*17,43,x+21+k*17,65),fill='#d9d6b9',outline='#776a4d',width=2);d.ellipse((x+10+k*17,47,x+18+k*17,58),fill='#574331')
   elif family=='bakery':
    for k in range(3):
     xx=x+5+k*19;d.ellipse((xx,34,xx+17,69),fill='#ba9b61',outline='#765c3d',width=2);d.arc((xx+3,40,xx+16,60),80,270,fill='#e2c28b',width=2)
   else:
    d.ellipse((x+7,33,x+43,73),fill='#c0b077',outline='#617649',width=3);d.rectangle((x+44,33,x+56,74),fill='#ac824d');d.line((x+6,82,x+59,82),fill=pale,width=2)
  d.line((14,16,236,16),fill='#e4dfbd',width=3)
 elif family=='travel':
  art.box(d,(23,23,89,77),'#c9c9a8','#738478',1)
  d.polygon([(29,61),(39,38),(61,43),(73,32),(83,61)],fill='#718f84');d.line((29,63,83,63),fill='#8cafb0',width=3)
  art.box(d,(137,22,215,69),dark,pale,2);art.box(d,(144,29,208,59),'#72918c',width=1);d.rectangle((169,70,181,83),fill='#a3aea2');d.line((152,86,202,86),fill='#c5c8ad',width=6)
 elif family=='stationery':
  for y in (23,61):
   for i,x in enumerate(range(23,221,28)):
    art.box(d,(x,y,x+21,y+28),['#b59b76','#749c96','#9f7e8b'][i%3],pale,1,0);d.line((x+4,y+7,x+16,y+7),fill='#e2d7af',width=2)
 elif family in ('beauty','market'):
  for y in (29,67):
   for i,x in enumerate(range(26,222,24)):
    color=['#b09d7c','#779b8c','#aa8391','#bac4a5'][i%4]
    art.box(d,(x,y,x+15,y+23),color,'#cfceb0',1);d.rectangle((x+4,y-6,x+11,y),fill='#283f40');d.line((x+3,y+11,x+12,y+11),fill='#dfd9b4',width=2)
 elif family=='home':
  for x in (28,91,165):
   d.ellipse((x,28,x+32,80),fill=pale,outline=dark,width=2);d.rectangle((x+8,20,x+24,36),fill=accent)
  d.line((17,93,235,93),fill='#bfb28a',width=3)
 elif family=='thrift':
  d.line((21,25,232,25),fill=pale,width=4)
  for i,x in enumerate(range(30,215,34)):
   d.polygon([(x,31),(x-9,43),(x-3,52),(x+1,47),(x+1,87),(x+25,87),(x+25,47),(x+30,52),(x+35,43),(x+24,31)],fill=['#9e8c69','#7b9181','#a17d75'][i%3],outline=dark)
 elif family=='retro':
  for x in (23,143):
   art.box(d,(x,18,x+80,94),'#7e6e57','#c6b78a',3);art.box(d,(x+8,25,x+72,78),'#294447','#a9b5a2',3)
   for y in range(32,73,6):d.line((x+13,y,x+67,y),fill='#6b9188',width=2)
   d.ellipse((x+12,83,x+20,91),fill='#c4ad79');d.line((x+35,87,x+66,87),fill=dark,width=3)
 return art.save(im,'env33b_fixture_'+name)
def flat(kind,accent):
 im=art.base();d=ImageDraw.Draw(im);rng=random.Random(33017)
 if kind=='leaves':
  for i in range(23):
   x,y=rng.randrange(10,110),rng.randrange(9,110);d.ellipse((x,y,x+13,y+6),fill=rng.choice(['#516f49','#748357','#8f9665']),outline='#354c3c')
 elif kind=='vinyl':
  for x,y in [(14,20),(67,63),(47,31)]:
   d.ellipse((x,y,x+35,y+35),fill='#334242',outline='#89937c',width=2);d.ellipse((x+13,y+13,x+21,y+21),fill=accent)
 elif kind=='cables':
  for i in range(3):d.arc((13+i*11,17+i*13,97+i*4,97+i*6),20,325,fill='#293c3e',width=3)
 elif kind=='ceramic':
  for i in range(10):
   x,y=rng.randrange(15,100),rng.randrange(15,100);d.polygon([(x,y),(x+12,y+3),(x+5,y+15)],fill='#c7c6a5',outline='#818c76')
 elif kind=='cloth':
  for x,y in [(17,23),(66,61),(34,65)]:d.polygon([(x,y),(x+28,y+4),(x+35,y+23),(x+8,y+29)],fill=accent,outline='#6b7b6a')
 elif kind=='threshold':
  for x in range(8,120,12):d.rectangle((x,16,x+6,112),fill=tuple(accent)+(125,))
 elif kind=='wayfinding':
  d.line((18,99,105,99),fill=tuple(accent)+(170,),width=6);d.polygon([(90,84),(112,99),(90,114)],fill=tuple(accent)+(170,))
 return art.save(im,'env33b_detail_'+kind)
def wallprint(family,accent):
 im=art.base();d=ImageDraw.Draw(im);a=rgb(accent);pale='#d2d2b0'
 d.rectangle((0,0,127,8),fill=a);d.line((0,9,127,9),fill=pale,width=2)
 art.box(d,(27,12,101,53),'#344b4a',pale,2,0)
 if family=='optical':
  d.ellipse((39,23,57,39),outline=pale,width=2);d.ellipse((66,23,84,39),outline=pale,width=2);d.line((57,31,66,31),fill=pale,width=2)
 elif family in ('records','retro'):
  d.ellipse((47,17,80,48),fill='#263538',outline=pale,width=2);d.ellipse((60,29,67,36),fill=accent)
 elif family=='bikes':
  for x in (36,70):d.ellipse((x,28,x+21,47),outline=pale,width=2)
  d.line((47,38,59,22,81,38,47,38,63,46,59,22),fill=pale,width=2)
 elif family=='garden':
  d.line((64,46,64,20),fill=pale,width=2)
  for x,y in [(45,23),(66,23),(43,35),(66,36)]:d.ellipse((x,y,x+19,y+8),fill=accent,outline=pale)
 elif family=='electronics':
  art.box(d,(43,18,87,45),'#73948d',pale,2,0);d.line((55,49,76,49),fill=pale,width=2)
 else:
  d.text((34,24),family.upper()[:8],font=art.font(10),fill=pale)
 return art.save(im,'env33b_wall_'+family)

def portal(name,accent):
 im=art.base();d=ImageDraw.Draw(im);a=rgb(accent)
 art.box(d,(8,9,119,119),'#273e40','#99aca2',3);art.box(d,(11,11,116,33),a,'#c8d0b4',1)
 art.box(d,(17,39,109,109),'#507572','#a2b6a6',2)
 for x in (24,69):
  d.rectangle((x,44,x+33,101),fill='#456563',outline='#bfd0bb',width=2);d.line((x+6,48,x+25,64),fill='#739590',width=2)
 d.line((60,37,60,110),fill='#c4c7a8',width=4)
 d.polygon([(41,76),(68,76),(68,65),(90,83),(68,101),(68,90),(41,90)],fill='#d5d4ac',outline='#355153')
 return art.save(im,'env33b_portal_'+name)
def main():
 art.main() # deterministic base; follow with this phase to rebuild the full pack.
 m=json.loads((OUT/'environment33.json').read_text());campaign=json.loads((ROOT/'content/campaigns/zombie-mall.finale.json').read_text());areas={a['id']:a for a in campaign['areas']}
 fixtures={};walls={}
 for family,accent in STORES:
  if family in fixtures:continue
  walls[family]=wallprint(family,accent)
  fixtures[family]={role:sprite(family+'-'+role,family,accent,role) for role in ['display','feature','seating','stock']}
 details={k:flat(k,(115,151,125)) for k in ['leaves','vinyl','cables','ceramic','cloth','threshold','wayfinding']}
 provenance=[]
 for i,(family,accent) in enumerate(STORES,1):
  aid=f'area.lower.{i:02}';a=areas[aid];front=a['zones'][0];keys=[]
  style=front['floorStyle']
  for v in range(2):
   im=Image.open(OUT/f'env33_floor_{style}_{v}.png').convert('RGBA')
   # Subtle unique material palette; retain physical five-foot tile scale.
   tint=Image.new('RGBA',im.size,accent+(255,));im=Image.blend(im,tint,.16 if family not in ('records','retro') else .35)
   keys.append(art.save(im,f'env33b_floor_{i:02}_{v}'))
  layout={'floors':{front['id']:keys},'decals':[],'signs':[],'features':{}}
  for prop in a['props']:
   kind=prop['kind'];role='stock' if '.back.' in prop['id'] else 'seating' if kind=='chair' else 'feature' if kind in ('counter','table','desk') else 'display'
   if kind in ('glass','spill','poster'):
    if kind=='poster':m['propOverrides'][prop['id']]='env33_prop_poster' if family=='travel' else walls[family]
    continue # Existing flat glass/spills remain visibly passable.
   m['propOverrides'][prop['id']]=fixtures[family][role]
  features={f['id']:f for f in a['features']}
  for suffix,label in [('guide',a['name'].upper()),('back-door','STOCKROOM'),('work-door','STAFF ONLY')]:
   f=features[f'feature.lower.{i:02}.{suffix}'];layout['signs'].append(dict(label=label,x=f['position']['x'],y=f['position']['y'],theme=style,color=list(accent),maxWidthTiles=4.5))
  route=features[f'route.lower.{i:02}.b'];layout['features'][route['id']]=portal(f'{i:02}',accent)
  # Curated low floor details only: no new furniture footprints, hidden clues or loot.
  used={(f['position']['x'],f['position']['y']) for f in a['features']}|{(e['position']['x'],e['position']['y']) for e in a['entryPoints']}
  for prop in a['props']:
   used|={(prop['position']['x']+dx,prop['position']['y']+dy) for dx in range(prop['width']) for dy in range(prop['height'])}
  floors={(p['x'],p['y']) for p in a['floors']}
  motif={'garden':'leaves','records':'vinyl','retro':'vinyl','electronics':'cables','beauty':'ceramic','bakery':'ceramic','coffee':'ceramic','thrift':'cloth','home':'cloth'}.get(family,'paper')
  coords=[(8,18),(14,20),(20,23),(28,22),(33,24),(36,18),(8,11),(20,12),(28,14),(35,10)]
  for j,(x,y) in enumerate(coords):
   if (x,y) not in floors or (x,y) in used:continue
   asset=details[motif] if motif in details else 'env33_decal_paper'
   if j%3==1:asset='env33_decal_tracks'
   layout['decals'].append(dict(texture=asset,x=x,y=y,width=1,height=1))
  for x in [4,8,16,20,27,35]:
   if (x,17) in floors and (x,17) not in used:layout['decals'].append(dict(texture=walls[family],x=x,y=17,width=1,height=1))
  m['areas'][aid]=layout
  source=next(s for s in campaign['lowerMall']['sites'] if s['number']==i)
  provenance.append({'number':i,'areaId':aid,'name':source['name'],'family':family,'sourcePages':[source['source']['pageStart'],source['source']['pageEnd']],'sourceCue':source['bookText'][:650],'frontFloor':keys,'signCount':len(layout['signs']),'decalCount':len(layout['decals'])})
 for aid in ['area.lower.concourse','area.lower.north','area.lower.east']:
  a=areas[aid];layout={'floors':{},'features':{},'decals':[],'signs':[]};floor={(c['x'],c['y']) for c in a['floors']}
  for prop in a['props']:
   if prop['kind'] in ('table','bench'):m['propOverrides'][prop['id']]=fixtures['home']['seating']
  for f in a['features']:
   if f['kind']!='transition':continue
   if aid=='area.lower.concourse' and f['id'].startswith('route.lower.') and f['id'].split('.')[2].isdigit():
    n=int(f['id'].split('.')[2]);layout['features'][f['id']]=f'env33b_portal_{n:02}';accent=STORES[n-1][1];label=areas[f'area.lower.{n:02}']['name'].upper()
   elif f['id'].startswith('route.lower.') and not f['id'].split('.')[2].isdigit():
    label='OFFICE REFUGE' if 'refuge' in f['id'] else 'CONTINUE LOWER MALL';accent=(87,134,126);layout['features'][f['id']]=portal('wayfinding',accent)
   else:continue # Later numbered storefronts belong to the next art batches.
   layout['signs'].append(dict(label=label,x=f['position']['x'],y=f['position']['y'],theme='shelter',color=list(accent),maxWidthTiles=3.6))
  blocked={(f['position']['x'],f['position']['y']) for f in a['features']}
  for prop in a['props']:blocked|={(prop['position']['x']+dx,prop['position']['y']+dy) for dx in range(prop['width']) for dy in range(prop['height'])}
  for x,y in sorted(floor):
   if (x,y) in blocked:continue
   if y%6==2 and x%7==0:layout['decals'].append(dict(texture=details['wayfinding'],x=x,y=y,width=1,height=1))
   elif (x*13+y*7)%101==0:layout['decals'].append(dict(texture='env33_decal_litter',x=x,y=y,width=1,height=1))
  m['areas'][aid]=layout
 (OUT/'environment33.json').write_text(json.dumps(m,indent=2)+'\n')
 (ROOT/'docs/environment-33b-coverage.json').write_text(json.dumps({'phase':'33B','locations':provenance,'connectorAreas':['area.lower.concourse','area.lower.north','area.lower.east'],'limits':'Display stock and floor dressing are cosmetic. Only locations 1–18 have individual authored art; later shop interiors remain for later batches.'},indent=2)+'\n')
 print('Built Phase 33B: 18 stores, three connecting concourses, original art, unchanged campaign geometry.')
if __name__=='__main__':main()
