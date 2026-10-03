"""
WebAssembly persistence check: open an artwork, start a survey, save a draft (writes patina.json to LocalFolder),
reload the page, and look for the draft badge on the collection list. Screens are saved per step.
Coordinates are measured on the 1280x800 viewport (two-column artwork layout).
"""
import sys, time, pathlib
from playwright.sync_api import sync_playwright

url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8123"
out = pathlib.Path(sys.argv[2] if len(sys.argv) > 2 else "_shots/wasm")
out.mkdir(parents=True, exist_ok=True)
steps = [s for s in (sys.argv[3] if len(sys.argv) > 3 else "").split(",") if s]

def wait_ready(page, seconds=90):
    deadline = time.time() + seconds
    while time.time() < deadline:
        if page.evaluate("() => !document.querySelector('#uno-body .uno-loader') && !!document.querySelector('canvas')"):
            time.sleep(4)
            return True
        time.sleep(1)
    return False

with sync_playwright() as p:
    browser = p.chromium.launch(channel="msedge", headless=True)
    # A persistent profile directory keeps IndexedDB across the reload, like a real browser.
    context = browser.new_context(viewport={"width": 1280, "height": 800})
    page = context.new_page()
    page.goto(url)
    print("ready", wait_ready(page))

    n = 0
    for step in steps:
        n += 1
        kind, _, arg = step.partition(":")
        if kind == "click":
            x, y = (int(v) for v in arg.split("x"))
            page.mouse.click(x, y)
            time.sleep(2.5)
        elif kind == "wheel":
            page.mouse.move(640, 400)
            page.mouse.wheel(0, int(arg))
            time.sleep(1.5)
        elif kind == "type":
            page.keyboard.type(arg, delay=30)
            time.sleep(1)
        elif kind == "key":
            page.keyboard.press(arg)
            time.sleep(1)
        elif kind == "wait":
            time.sleep(float(arg))
        elif kind == "reload":
            # Navigation pushes deep links (/Main/Collection); a static server without an SPA fallback 404s on
            # page.reload(), so reopen the root like a returning user would.
            page.goto(url)
            print("reloaded, ready", wait_ready(page))
        page.screenshot(path=str(out / f"p{n:02d}-{kind}.png"))
        print(f"step {n}: {step}")

    stored = page.evaluate("""async () => {
        const dbs = indexedDB.databases ? await indexedDB.databases() : [];
        return dbs.map(d => d.name).join(',');
    }""")
    print("indexedDB databases:", stored)
    browser.close()
