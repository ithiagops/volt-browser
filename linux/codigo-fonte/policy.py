"""Linux port of VOLT Windows 0.6.0 resource and tab policies."""
from urllib.parse import urlparse, quote
from pathlib import Path
import os
TRACKERS = ('google-analytics.com','googletagmanager.com','doubleclick.net','hotjar.com','hotjar.io','clarity.ms')
def address(text):
    text=text.strip()
    if urlparse(text).scheme in ('http','https'): return text
    if not any(c.isspace() for c in text) and ('.' in text or text.startswith('localhost')):
        return 'https://'+text
    return 'https://www.google.com/search?q='+quote(text)
def is_tracker(url):
    host=(urlparse(url).hostname or '').lower()
    return any(host==d or host.endswith('.'+d) for d in TRACKERS)
def move_block(order, selected, slot):
    chosen=set(selected); group=[x for x in order if x in chosen]
    rest=[x for x in order if x not in chosen]
    slot=max(0,min(slot,len(order)))
    target=slot-sum(x in chosen for x in order[:slot])
    return rest[:target]+group+rest[target:]
def survivor(order, selected, active):
    remaining=[x for x in order if x not in selected]
    if active in remaining: return active
    if not remaining:return None
    at=order.index(active) if active in order else 0
    return next((x for x in order[at+1:] if x in remaining),remaining[-1])
def eligible(visible, ready, loading, pinned, downloads, audio):
    return not (visible or not ready or loading or pinned or downloads or audio)
def pressure(was_active, used):return used > (1_000_000_000 if was_active else 1_200_000_000)
def process_memory():
    """Sum Linux RSS of this process tree. Shared pages may be counted twice."""
    records={}
    for p in Path('/proc').iterdir():
        if not p.name.isdigit():continue
        try:
            stat=(p/'stat').read_text().rsplit(')',1)[1].split()
            records[int(p.name)]=(int(stat[1]),int(stat[21])*os.sysconf('SC_PAGE_SIZE'))
        except (OSError,ValueError,IndexError):pass
    owned={os.getpid()}
    while True:
        new=owned|{pid for pid,(parent,_) in records.items() if parent in owned}
        if new==owned:break
        owned=new
    return sum(records.get(pid,(0,0))[1] for pid in owned)


def sleep_due(minutes, seconds):
    return minutes > 0 and seconds >= minutes*60

def position_priority(index, count):
    return 0 <= index < max(0,min(100,count))

def safe_history_url(url):
    if not isinstance(url,str) or len(url)>2048:return False
    try:
        parsed=urlparse(url)
        return parsed.scheme in ('http','https') and bool(parsed.hostname) and not parsed.username and not parsed.password
    except ValueError:return False
