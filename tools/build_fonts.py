"""Regenerate the bundled bitmap font atlases (Pillow and DejaVu fonts required)."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
root=Path(__file__).resolve().parents[1]/'src/Ashen.Game/Assets'
for name,cell,size,path in [('font',32,19,'/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'),('title',64,42,'/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf')]:
    f=ImageFont.truetype(path,size)
    im=Image.new('RGBA',(cell*16,cell*6),(0,0,0,0));d=ImageDraw.Draw(im)
    widths=[]
    for i in range(96):
        c=chr(i+32);d.text(((i%16)*cell,(i//16)*cell),c,font=f,fill='white',stroke_width=0)
        widths.append(d.textlength(c,font=f))
    im.save(root/(name+'.png'))
    (root/(name+'.json')).write_text(json.dumps(widths))
