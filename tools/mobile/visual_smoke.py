"""Exercise the built Android app through adb and retain real emulator screenshots."""
from __future__ import annotations
import base64
import io
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageDraw

PACKAGE='com.tekatz.blackjackcasino.mobile'
OUT=Path('visual-proof');OUT.mkdir(exist_ok=True)
shots=[]

def adb(*args, timeout=40):
    try:
        return subprocess.check_output(['adb',*args],timeout=timeout,stderr=subprocess.STDOUT).decode('utf-8',errors='replace')
    except subprocess.CalledProcessError as error:
        print('ADB FAILURE:',error.output.decode('utf-8',errors='replace'),flush=True)
        raise

def hierarchy():
    adb('shell','uiautomator','dump','/sdcard/club-window.xml')
    return ET.fromstring(adb('shell','cat','/sdcard/club-window.xml'))

def nodes():
    root=hierarchy()
    (OUT/'latest-window.xml').write_bytes(ET.tostring(root))
    return list(root.iter('node'))

def bounds(node):
    values=[int(v) for v in re.findall(r'\d+',node.get('bounds',''))]
    return values if len(values)==4 else [0,0,0,0]

def find(needles, scroll=False, wait=12):
    end=time.monotonic()+wait; attempts=0
    while time.monotonic()<end:
        for n in nodes():
            fields=[n.get('text',''),n.get('content-desc',''),n.get('resource-id','')]
            if any(needle.casefold()==field.casefold() or field.endswith('/'+needle) for needle in needles for field in fields):
                x1,y1,x2,y2=bounds(n)
                if x2>x1 and y2>y1 and n.get('enabled','true')=='true':return (x1+x2)//2,(y1+y2)//2
        if scroll:
            adb('shell','input','swipe','500','1700','500','700','350');attempts+=1
        time.sleep(.3)
    raise RuntimeError('Could not find '+repr(needles))

def tap(*needles, scroll=False, wait=12):
    x,y=find(needles,scroll,wait);adb('shell','input','tap',str(x),str(y));time.sleep(.45)

def shot(name):
    time.sleep(.5)
    path=OUT/(name+'.png')
    path.write_bytes(subprocess.check_output(['adb','exec-out','screencap','-p']))
    with Image.open(path) as im:
        if im.width<600 or im.height<600:raise RuntimeError('Invalid screenshot size')
    shots.append(path);print('SCREENSHOT',name,flush=True)

def home():
    adb('shell','am','force-stop',PACKAGE)
    resolved=adb('shell','cmd','package','resolve-activity','--brief','-a','android.intent.action.MAIN','-c','android.intent.category.LAUNCHER',PACKAGE)
    print('LAUNCH COMPONENT:',resolved,flush=True)
    component=next((line.strip() for line in resolved.splitlines() if '/' in line and PACKAGE in line),None)
    if component is None:raise RuntimeError('No launcher component: '+resolved)
    print(adb('shell','am','start','-W','-n',component),flush=True)
    find(['HomeTab','A different kind of night.'],wait=40);time.sleep(.4)

def main():
    apk=next(Path('test-apk').glob('*.apk'))
    print(adb('install','-r',str(apk),timeout=120),flush=True)
    print(adb('shell','pm','list','packages',PACKAGE),flush=True)
    home();shot('01-lobby')
    tap('OpenBlackjack','Blackjack');shot('02-blackjack-ready')
    tap('DealCards','DEAL CARDS');time.sleep(.9);shot('03-blackjack-deal')
    # Finish the test hand so other cash games can be exercised honestly.
    for _ in range(5):
        fields=[n.get('text','') for n in nodes()]
        if 'NO INSURANCE' in fields:tap('DeclineInsurance','NO INSURANCE')
        elif 'STAND' in fields:tap('Stand','STAND')
        else:break
        time.sleep(.5)
    for game,title,index in [('Reels','Midnight Reels',4),('NeonVault','Neon Vault',5),('NineLives','Nine Lives',6)]:
        home();tap('OpenSlot-'+game,title,scroll=True)
        xy=find(['SpinSlot','SPIN · $10.00'])
        recorder=subprocess.Popen(['adb','shell','screenrecord','--size','720x1600','--bit-rate','1000000','--time-limit','5','/sdcard/'+game+'.mp4'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        time.sleep(.4);adb('shell','input','tap',str(xy[0]),str(xy[1]));time.sleep(2.6)
        shot(f'{index:02d}-'+game.lower());recorder.wait(timeout=10)
        adb('pull','/sdcard/'+game+'.mp4',str(OUT/(game+'.mp4')))
        if game=='NeonVault':
            adb('shell','settings','put','system','accelerometer_rotation','0');adb('shell','settings','put','system','user_rotation','1');time.sleep(1.2);shot('05b-neon-landscape')
            adb('shell','settings','put','system','user_rotation','0');time.sleep(.8)
    home();tap('OpenMidnightDice','Midnight Dice');tap('DiceCasual','START CASUAL GAME');tap('RollDice','ROLL FIVE DICE');time.sleep(.8);shot('07-dice')
    home();tap('OpenAlbumTab','Album');shot('08-album')
    tap('AlbumPacks','PACKS');tap('OpenPack-Common','OPEN PACK',scroll=True);shot('09-pack')
    tap('RevealNextCard','REVEAL CARD 1 / 5');time.sleep(.9);shot('10-pack-reveal')
    home();tap('OpenProfileTab','You');shot('11-profile')
    home();tap('OpenCasinoVault','Casino Vault',scroll=True);shot('12-vault')
    adb('logcat','-d','-s','AndroidRuntime:E')
    contact=Image.new('RGB',(1400,4*820),'#0A1116');draw=ImageDraw.Draw(contact)
    for i,path in enumerate(shots[:16]):
        with Image.open(path) as im:
            im.thumbnail((340,775));x=(i%4)*350;y=(i//4)*820;contact.paste(im.convert('RGB'),(x,y+24));draw.text((x+4,y+5),path.stem,fill='white')
    contact=contact.crop((0,0,1400,((len(shots)+3)//4)*820));contact.save(OUT/'contact.jpg',quality=83)
    (OUT/'report.json').write_text(json.dumps({'package':PACKAGE,'screenshots':[p.name for p in shots],'screen_count':len(shots),'input':'native UI / adb','financial_state':'isolated test profile'},indent=2)+'\n')
    for label,path in [('CONTACT',OUT/'contact.jpg'),('NEON',OUT/'05-neonvault.png')]:
        with Image.open(path) as im:
            if label=='NEON':im.thumbnail((720,1600))
            b=io.BytesIO();im.convert('RGB').save(b,'JPEG',quality=78)
        text=base64.b64encode(b.getvalue()).decode()
        for i in range(0,len(text),4096):print(f'CLUB_PREVIEW_{label}:{i//4096:04d}:'+text[i:i+4096],flush=True)
    print('PASS: real Android UI screenshots and reel-motion recordings captured.',flush=True)

if __name__=='__main__':
    try:main()
    except Exception:
        try:
            shot('FAILED');(OUT/'logcat.txt').write_text(adb('logcat','-d'));print(adb('logcat','-d','-s','AndroidRuntime:E','ActivityTaskManager:E'),flush=True)
        except Exception:pass
        raise
