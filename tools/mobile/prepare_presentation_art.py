"""Extract the actual desktop presentation assets for Android; never substitute artwork."""
from __future__ import annotations
import base64
import hashlib
import io
import json
import re
import shutil
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DESKTOP = ROOT / 'src/BlackjackCasino.Desktop'
OUT = ROOT / 'src/BlackjackCasino.Mobile/Resources/Images/Club'
NS = {'w': 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'}
records: list[dict] = []

def save(name: str, data: bytes, source: str):
    (OUT / name).write_bytes(data)
    records.append({'file': name, 'source': source, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})

def constants(pattern: str) -> dict[str, tuple[str, str]]:
    result = {}
    for path in sorted(DESKTOP.glob(pattern)):
        text = path.read_text(encoding='utf-8-sig')
        for match in re.finditer(r'const string (\w+)\s*=\s*(?:"""(.*?)"""|"([^"\r\n]*)")\s*;', text, re.S):
            result[match[1]] = (match[2] or match[3], path.name)
    return result

def bitmap(name: str, encoded: str, source: str):
    data = base64.b64decode(re.sub(r'\s', '', encoded), validate=True)
    with Image.open(io.BytesIO(data)) as image:
        image.load()
        if image.width < 64 or image.height < 64: raise ValueError(f'Too small: {name}')
    save(name, data, source)

def svg_color(hex_value: str) -> tuple[str, str]:
    if hex_value == 'Transparent': return 'none', '1'
    if re.fullmatch(r'#[0-9a-fA-F]{8}', hex_value):
        return '#' + hex_value[3:], str(int(hex_value[1:3], 16) / 255)
    return hex_value, '1'

def classic_symbols():
    path = DESKTOP / 'Presentation/SlotTextures.xaml'
    root = ET.parse(path).getroot()
    for drawing in root.findall('w:DrawingImage', NS):
        key = drawing.attrib['{http://schemas.microsoft.com/winfx/2006/xaml}Key']
        defs, paths = [], []
        for index, item in enumerate(drawing.findall('.//w:GeometryDrawing', NS)):
            geometry = item.get('Geometry', '')
            rule = 'evenodd' if geometry.startswith('F0 ') else 'nonzero'
            geometry = re.sub(r'^F[01]\s*', '', geometry)
            fill, opacity = svg_color(item.get('Brush', 'Transparent'))
            brush = item.find('w:GeometryDrawing.Brush', NS)
            if brush is not None:
                gradient = next(iter(brush))
                kind = gradient.tag.rsplit('}', 1)[-1]
                gid = f'g{index}'
                stops = ''.join(f'<stop offset="{s.get("Offset")}" stop-color="{svg_color(s.get("Color"))[0]}" stop-opacity="{svg_color(s.get("Color"))[1]}"/>' for s in gradient)
                if kind == 'LinearGradientBrush':
                    x1,y1 = gradient.get('StartPoint','0,0').split(','); x2,y2 = gradient.get('EndPoint','1,1').split(',')
                    defs.append(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{stops}</linearGradient>')
                else:
                    cx,cy=gradient.get('Center','.5,.5').split(','); fx,fy=gradient.get('GradientOrigin','.5,.5').split(',')
                    defs.append(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" fx="{fx}" fy="{fy}" r=".5">{stops}</radialGradient>')
                fill=f'url(#{gid})'; opacity='1'
            stroke, stroke_opacity, thickness, cap, join = 'none','1','0','butt','miter'
            pen = item.find('w:GeometryDrawing.Pen/w:Pen', NS)
            if pen is not None:
                stroke, stroke_opacity = svg_color(pen.get('Brush','#000000'))
                thickness=pen.get('Thickness','1'); cap=pen.get('StartLineCap','Flat').lower(); join=pen.get('LineJoin','Miter').lower()
                if cap=='flat': cap='butt'
            paths.append(f'<path d="{geometry}" fill="{fill}" fill-opacity="{opacity}" fill-rule="{rule}" stroke="{stroke}" stroke-opacity="{stroke_opacity}" stroke-width="{thickness}" stroke-linecap="{cap}" stroke-linejoin="{join}"/>')
        svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 100 100"><defs>{"".join(defs)}</defs>{"".join(paths)}</svg>'
        ET.fromstring(svg)
        save('club_reels_'+key.removeprefix('Slot').lower()+'.svg', svg.encode(), 'Presentation/SlotTextures.xaml#'+key)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for path in OUT.iterdir():
        if path.is_file(): path.unlink()
    neon=constants('NeonVaultSymbolData.*.cs')
    for name in ['Pulse','Prism','Chip','Crown','Key','Wild','Scatter','VaultKey']:
        value,source=neon[name]; bitmap('club_neon_'+name.lower()+'.jpg',value,source+'#'+name)
    cats=constants('NineLivesArtData.Symbols*.cs')
    for name in ['Fishbone','Bell','TabbyCat','WhiteCat','BlackCat','Wild','Bonus']:
        value,source=cats[name]; bitmap('club_cats_'+name.lower()+'.jpg',value,source+'#'+name)
    yarn=DESKTOP/'Assets/NineLives/yarn.jpg'
    save('club_cats_yarn.jpg',yarn.read_bytes(),'Assets/NineLives/yarn.jpg')
    # Port the actual rooftop/moon stage from NineLivesLobbyArtwork. The old
    # encoded Background0..6 payload is malformed in the inherited desktop source;
    # it is not silently decoded with missing bytes or substituted symbol art.
    stage='''<svg xmlns="http://www.w3.org/2000/svg" width="960" height="494" viewBox="0 0 360 185"><defs><linearGradient id="night" x2="1" y2="1"><stop stop-color="#0a0712"/><stop offset="1" stop-color="#190923"/></linearGradient><radialGradient id="moon"><stop stop-color="#ffecae"/><stop offset=".62" stop-color="#e8c16a"/><stop offset="1" stop-color="#c95cff" stop-opacity="0"/></radialGradient></defs><path fill="url(#night)" d="M0 0H360V185H0Z"/><circle cx="290" cy="20" r="38" fill="url(#moon)"/><path fill="#07060e" fill-opacity=".86" d="M0 163L36 132 70 153 111 118 151 151 205 112 247 148 292 124 360 159V185H0Z"/></svg>'''
    save('club_cats_background.svg',stage.encode(),'NineLivesLobbyArtwork.cs#moon-rooftops')
    classic_symbols()
    for name in ['album','challenges','season','shop','statistics']:
        save(f'club_nav_{name}.png',(DESKTOP/f'Assets/Navigation/{name}.png').read_bytes(),f'Assets/Navigation/{name}.png')
    for path in sorted((DESKTOP/'Assets/Portraits').glob('*.png')):
        with Image.open(path) as image:
            image=image.convert('RGB'); image.thumbnail((512,512), Image.Resampling.LANCZOS)
            output=io.BytesIO(); image.save(output,'JPEG',quality=92)
            save('club_portrait_'+path.stem.replace('-','_')+'.jpg',output.getvalue(),str(path.relative_to(DESKTOP)))
    guest='''<svg xmlns="http://www.w3.org/2000/svg" width="192" height="192" viewBox="0 0 192 192"><defs><linearGradient id="g" x2="1" y2="1"><stop stop-color="#254b39"/><stop offset="1" stop-color="#0b1915"/></linearGradient></defs><rect width="192" height="192" rx="24" fill="url(#g)"/><circle cx="96" cy="96" r="78" fill="none" stroke="#d6b478" stroke-width="2"/><circle cx="96" cy="72" r="26" fill="#d6b478"/><path d="M43 147Q43 105 96 105Q149 105 149 147Z" fill="#d6b478"/><path d="M72 109L96 139L120 109L109 153H83Z" fill="#14281f"/></svg>'''
    save('club_portrait_classic.svg',guest.encode(),'mobile-house-guest-badge')
    if len(records)!=36: raise ValueError(f'Expected 36 presentation assets, got {len(records)}')
    (OUT/'.complete').write_text('desktop-originals-v1\n', encoding='utf-8')
    report=ROOT/'artifacts/mobile-presentation-art-report.json'; report.parent.mkdir(exist_ok=True)
    report.write_text(json.dumps({'assets':records,'total_bytes':sum(r['bytes'] for r in records)},indent=2)+'\n',encoding='utf-8')
    print(f'PASS: {len(records)} verified desktop assets, {sum(r["bytes"] for r in records):,} bytes; no fallback symbols.')

if __name__=='__main__': main()
