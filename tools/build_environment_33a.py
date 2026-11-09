#!/usr/bin/env python3
"""Original code-authored environment assets. Pillow is needed only to rebuild art.
All coordinates are in a 128 px five-foot tile. No campaign/save data is edited.
"""
from pathlib import Path
import json, random, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'src/Ashen.Game/Assets'
S=128
font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
def font(n):
    try:return ImageFont.truetype(font_path,n)
    except OSError:return ImageFont.load_default()
def save(im,key): im.save(OUT/(key+'.png'),optimize=True);return key
def base(w=128,h=128):return Image.new('RGBA',(w,h),(0,0,0,0))
def box(d,r,fill,outline='#263334',width=2,radius=3):
    d.rounded_rectangle(tuple(int(x) for x in r),radius,fill,outline,width)
def floor(style,variant):
    colors={'asphalt':(67,72,73),'concrete':(137,142,136),'concourse':(180,177,158),'vestibule':(106,127,126),'carpet':(69,100,111),'paper':(179,157,119),'cafe':(127,86,55),'gourmet':(166,174,144),'shelter':(84,110,95)}
    c=colors[style];rng=random.Random(33000+variant+list(colors).index(style)*100)
    im=base();p=im.load()
    for y in range(S):
        for x in range(S):
            n=rng.randint(-5,5)
            if style in ('carpet','shelter'):n+=((x+y)%3-1)*5
            if style=='asphalt':n*=1.5
            p[x,y]=tuple(max(0,min(255,int(a+n))) for a in c)+(255,)
    d=ImageDraw.Draw(im)
    if style=='cafe':
        for y in range(0,128,32):
            d.line((0,y,127,y),fill='#4f3a2b',width=2)
            d.line(((y//32%2)*64,y,(y//32%2)*64,min(127,y+32)),fill='#65432e',width=2)
            for k in range(8):
                yy=y+rng.randrange(3,30);xx=rng.randrange(128)
                d.line((xx,yy,min(xx+rng.randrange(10,40),127),yy),fill=(142,98,63,255))
    elif style in ('carpet','shelter'):
        for x in range(8,128,16):d.line((x,0,x,127),fill=tuple(max(0,a-7) for a in c)+(255,))
    elif style not in ('asphalt','concrete'):
        for x in (0,64):
            d.line((x,0,x,127),fill='#656f67',width=2);d.line((x+2,1,x+2,127),fill=tuple(min(255,a+12) for a in c)+(255,))
        for y in (0,64):d.line((0,y,127,y),fill='#657067',width=2)
        if style=='concourse':
            for i in range(95):
                x,y=rng.randrange(128),rng.randrange(128);d.rectangle((x,y,x+1,y+1),fill=(112,119,108,255))
        if style=='gourmet':
            for y in (0,64):
                for x in (0,64):
                    d.rectangle((x+26,y+26,x+35,y+35),fill='#687c6d')
    else:
        for i in range(10):
            x,y=rng.randrange(128),rng.randrange(128);d.line((x,y,min(127,x+8),y+1),fill=tuple(max(0,a-9) for a in c)+(255,))
    # Subtle, seeded wear; no baked lighting or random per-frame noise.
    for k in range(5):
        x,y=rng.randrange(6,120),rng.randrange(6,120)
        d.line((x,y,min(127,x+rng.randrange(2,12)),min(127,y+2)),fill=tuple(max(0,a-12) for a in c)+(255,))
    return save(im,f'env33_floor_{style}_{variant}')
def prop(kind):
    im=base(256,128);d=ImageDraw.Draw(im)
    # Shadows stay within the gameplay footprint.
    box(d,(9,15,251,121),(0,0,0,66),None,radius=10)
    if kind in ('desk','office-desk'):
        box(d,(6,9,246,110),'#70523c');box(d,(9,9,243,100),'#b18a60', '#d0ad7d')
        for y in (26,58,85):d.line((12,y,239,y+1),fill='#a17c53')
        box(d,(90,24,155,65),'#303e42');box(d,(96,29,149,58),'#496169')
        d.rectangle((117,65,129,74),fill='#4b5554');box(d,(102,77,149,87),'#c3c5b8',width=1)
        for i in range(5):d.line((105+i*8,79,105+i*8,85),fill='#66716b')
        for i in range(3):box(d,(24+i*4,40+i*5,66+i*4,70+i*5),'#d9d3b2','#858b78',1,0)
        d.ellipse((191,25,211,45),fill='#e3d6b3',outline='#594936',width=3);d.ellipse((196,30,206,40),fill='#543b2a')
    elif kind in ('shelf','food-shelf','fridge'):
        box(d,(6,6,246,114),'#33474a');box(d,(10,9,242,109),'#8c9a91','#b8bdb1')
        for y in (16,49,82):
            box(d,(15,y,237,y+27),'#3e4e4b',width=1)
            rng=random.Random(33+y+(kind=='food-shelf'))
            for x in range(21,228,19):
                color=rng.choice(['#b37854','#b7b19a','#689388','#b4a267','#85939f'])
                box(d,(x,y+4,x+12,y+22),color,'#263d3c',1,1)
                d.line((x+2,y+10,x+10,y+10),fill='#d4d1bb',width=2)
            d.line((14,y+27,238,y+27),fill='#ced1bd',width=3)
        if kind=='fridge':
            for x in (9,87,166):
                d.rectangle((x,8,x+77,110),fill=(108,177,185,65),outline='#cfdbd1',width=3)
                d.rectangle((x+66,46,x+69,74),fill='#293e42')
    elif kind in ('counter','coffee-counter','deli-counter'):
        box(d,(5,5,247,116),'#557576','#b1c0b2');box(d,(9,8,243,94),'#b3b4a1','#d2d3bd')
        for x in range(20,245,43):d.line((x,99,x,111),fill='#203d43',width=2)
        if kind=='coffee-counter':
            box(d,(22,18,105,70),'#414b4c','#e3d5b6',3)
            for x in (34,60,86):
                d.ellipse((x,25,x+10,35),fill='#b6c2ae');box(d,(x-2,48,x+13,62),'#dad9bd',width=1)
            box(d,(137,22,203,74),'#758b85','#283a3a');d.rectangle((143,29,197,57),fill='#283b3b')
            for i in range(4):d.ellipse((147+i*13,64,155+i*13,72),fill='#c8c5a8')
        elif kind=='deli-counter':
            for x in range(18,220,49):
                box(d,(x,22,x+41,70),'#5e7771','#d8d9c1')
                for k in range(4):d.ellipse((x+5+k*6,37,x+15+k*6,57),fill=['#a17d4d','#8e9b5b','#a75d45','#c5a36b'][k])
            d.line((12,17,239,17),fill='#e4e2c5',width=3)
        else:
            box(d,(150,21,218,67),'#314f53','#ccd0b7');box(d,(161,28,206,51),'#6c938b',width=1)
            for i in range(3):box(d,(29+i*30,28,51+i*30,62),'#c4b89a',width=1)
    elif kind=='cot':
        box(d,(5,6,248,115),'#4c5b59','#a6b3a5');box(d,(12,12,240,107),'#6c8d80','#d2d4b8')
        box(d,(17,18,71,100),'#dbd9ba','#96a697');box(d,(77,16,235,104),'#577a6a','#8ca28d')
        for x in range(94,236,28):d.line((x,20,x-7,99),fill='#648675',width=2)
        d.line((78,21,232,21),fill='#b3bea0',width=3)
    elif kind=='crate':
        for x in (6,129):
            box(d,(x,12,x+115,113),'#8a7251','#bca57a');box(d,(x+8,20,x+107,103),'#a38c65','#675a46')
            d.line((x+43,21,x+43,103),fill='#c4b18a',width=9);d.line((x+55,21,x+55,103),fill='#7d6b4d',width=1)
            box(d,(x+71,53,x+98,79),'#d0c7a3','#746b52',1,0)
            for y in (60,65,70):d.line((x+75,y,x+92,y),fill='#686a58')
    elif kind=='table':
        d.ellipse((4,9,249,116),fill='#4e4133',outline='#322f29',width=3)
        d.ellipse((9,5,245,105),fill='#af8c60',outline='#dec49a',width=3)
        d.arc((17,12,237,97),10,170,fill='#9c794c',width=2)
        d.ellipse((61,35,95,69),fill='#d6d5b8',outline='#69684e',width=2);d.ellipse((68,41,88,61),fill='#655039')
        box(d,(145,28,187,65),'#d6cfaa','#756852',1,0)
    elif kind=='chair':
        im=base();d=ImageDraw.Draw(im)
        box(d,(29,20,103,117),(0,0,0,60),None)
        box(d,(23,20,97,99),'#685641','#ab9470');box(d,(27,31,93,94),'#927754','#c6ae81')
        box(d,(20,10,100,33),'#695640','#d0b78d');d.line((36,43,82,43),fill='#ae956b',width=2)
    elif kind=='planter':
        im=base();d=ImageDraw.Draw(im)
        d.ellipse((12,17,120,124),fill=(0,0,0,64));d.ellipse((8,10,117,117),fill='#7e8270',outline='#bac0a3',width=4);d.ellipse((19,21,106,105),fill='#443f2e')
        rng=random.Random(337)
        for i in range(24):
            x,y=rng.randint(27,88),rng.randint(25,87)
            d.line((64,64,x,y),fill='#6d7951',width=3);d.ellipse((x-10,y-5,x+10,y+5),fill=rng.choice(['#586b42','#758451','#839156','#475e3c']),outline='#364b35')
    elif kind=='poster':
        box(d,(7,7,246,112),'#47585b','#acaa8d')
        for i,c in enumerate(['#799b99','#b4a07c','#798c6b']):
            x=16+i*77;box(d,(x,15,x+68,104),'#ded6b5','#283e43',1,0)
            d.rectangle((x+5,20,x+63,66),fill=c);d.polygon([(x+5,64),(x+25,32),(x+45,64)],fill='#d5d4b3')
            for y in (75,82,89):d.line((x+9,y,x+56,y),fill='#566b65',width=2)
    elif kind=='coffee-machine':
        box(d,(10,13,244,114),'#566867','#abb6a7');box(d,(21,22,233,94),'#9ba79c','#e2dfbf')
        for x in (43,107,171):
            d.ellipse((x,31,x+35,66),fill='#293b3a',outline='#d2d3b7',width=3);d.rectangle((x+13,60,x+20,84),fill='#3e5150')
        d.line((30,99,223,99),fill='#273e40',width=4)
    return save(im,'env33_prop_'+kind)
def decal(kind):
    im=base();d=ImageDraw.Draw(im)
    if kind in ('litter','paper'):
        rng=random.Random(331)
        for i in range(6):
            x,y=rng.randrange(12,90),rng.randrange(12,95)
            d.polygon([(x,y),(x+22,y+3),(x+19,y+18),(x-2,y+13)],fill='#c9c8ac',outline='#818976')
            d.line((x+3,y+6,x+16,y+7),fill='#88938a',width=1)
    elif kind=='spill':
        d.ellipse((14,23,101,87),fill=(67,55,39,110));d.ellipse((58,67,113,103),fill=(66,60,36,95))
        d.arc((17,26,97,83),200,290,fill=(153,143,105,100),width=2)
    elif kind=='glass':
        rng=random.Random(332)
        for i in range(13):
            x,y=rng.randrange(9,110),rng.randrange(9,108)
            d.polygon([(x,y),(x+9,y+3),(x+3,y+12)],fill=(125,182,181,128),outline=(189,220,203,160))
    elif kind=='mat':
        box(d,(6,8,121,119),'#3c504d','#899a83',3)
        for y in range(15,114,6):d.line((13,y,113,y),fill='#293d3c',width=2)
    elif kind=='tracks':
        for y in range(10,117,27):
            d.ellipse((43,y,53,y+16),fill=(44,54,47,75));d.ellipse((73,y+10,83,y+26),fill=(44,54,47,75))
    elif kind=='rubble':
        rng=random.Random(333)
        for i in range(9):
            x,y=rng.randrange(6,100),rng.randrange(8,96)
            d.polygon([(x,y),(x+18,y+2),(x+22,y+19),(x-3,y+15)],fill='#8d9182',outline='#4b5b54')
    return save(im,'env33_decal_'+kind)
def feature(kind):
    im=base();d=ImageDraw.Draw(im)
    if kind in ('container','empty','stockpile','workbench'):
        box(d,(12,24,116,114),(0,0,0,75),None)
        box(d,(8,17,110,105),'#6f644c','#c5b58b',3)
        box(d,(13,22,105,92),'#9e8862','#4c5142')
        for x in (25,86):d.rectangle((x,21,x+6,94),fill='#ccbc8f')
        box(d,(43,45,74,71),'#d2c9a4','#6c715d',1)
        if kind=='empty':
            box(d,(18,27,100,87),'#343e37','#827e60');d.line((23,34,96,34),fill='#b1a080',width=3)
        if kind=='stockpile':
            box(d,(43,42,78,79),'#426b59','#d4d4b0');d.rectangle((57,48,64,72),fill='#e4dcb4');d.rectangle((48,56,72,63),fill='#e4dcb4')
        if kind=='workbench':
            d.line((35,46,78,77),fill='#334b4a',width=8);d.arc((64,62,88,86),90,320,fill='#334b4a',width=6)
    else:
        box(d,(23,9,108,119),(0,0,0,60),None)
        box(d,(17,5,103,111),'#42636a','#b3c3af',3);box(d,(24,13,96,99),'#d3cfad','#2c4548')
        if kind=='directory':
            d.rectangle((28,18,92,36),fill='#416d6c');d.text((32,21),'MALL',font=font(12),fill='#dfddbb')
            for x,y in [(31,44),(58,44),(31,68),(58,68)]:box(d,(x,y,x+20,y+16),'#7b9986','#527b73',1,0)
            d.ellipse((71,78,79,86),fill='#c47e4a')
        else:
            for i in range(3):
                box(d,(31+i*4,22+i*22,85+i*2,41+i*22),'#e7dfba','#9a9f85',1,0)
                d.line((37+i*4,29+i*22,78,29+i*22),fill='#6c8678',width=2)
    return save(im,'env33_feature_'+kind)
def main():
    OUT.mkdir(exist_ok=True)
    manifest={'version':1,'floors':{},'props':{},'propOverrides':{},'features':{},'areas':{}}
    for style in ['asphalt','concrete','concourse','vestibule','carpet','paper','cafe','gourmet','shelter']:
        manifest['floors'][style]=[floor(style,v) for v in range(4)]
    for kind in ['desk','shelf','counter','cot','crate','table','chair','planter','poster']:
        manifest['props'][kind]=prop(kind)
    for kind in ['office-desk','food-shelf','fridge','coffee-counter','deli-counter','coffee-machine']:prop(kind)
    for id,kind in [('travel-a','desk'),('travel-b','desk'),('office-desk','office-desk'),('barista','coffee-counter'),('gourmet-counter','deli-counter'),('fridge','fridge'),('cafe-machine','coffee-machine')]:manifest['propOverrides']['prop.mall.'+id]='env33_prop_'+kind
    for i in range(3):manifest['propOverrides']['prop.mall.gourmet-shelf'+str(i)]='env33_prop_food-shelf'
    for kind in ['litter','paper','spill','glass','mat','tracks','rubble']:decal(kind)
    for kind in ['glass','spill','rubble']:manifest['props'][kind]='env33_decal_'+kind
    for kind in ['container','empty','stockpile','workbench','directory','notice']:manifest['features'][kind]=feature(kind)
    def mark(asset,x,y,w=1,h=1):return {'texture':'env33_decal_'+asset,'x':x,'y':y,'width':w,'height':h}
    decals=[mark('mat',2,15,2,2),mark('tracks',6,15,2,2),mark('litter',9,17),mark('paper',17,15),mark('litter',29,17),mark('glass',12,14),mark('spill',15,10),mark('paper',29,20),mark('paper',35,24),mark('tracks',12,23),mark('mat',12,26),mark('paper',14,27),mark('litter',20,20),mark('spill',36,10),mark('glass',32,19)]
    signs=[]
    for label,x,y,c in [('BREWED AWAKENING',13,13,'cafe'),('GOURMET GLUTTON',32,13,'gourmet'),('WANDERLUST GETAWAYS',13,18,'carpet'),('PAPER HAVEN',32,18,'paper'),('OFFICE REFUGE',12,25,'shelter')]:signs.append({'label':label,'x':x,'y':y,'theme':c})
    manifest['areas']['area.mall.southwest']={'decals':decals,'signs':signs}
    (OUT/'environment33.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('Built Phase 33A materials, furniture, details and presentation manifest.')
if __name__=='__main__':main()
