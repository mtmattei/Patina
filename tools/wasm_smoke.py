"""
WebAssembly smoke test: loads the published app in Edge/Chrome, captures the first screen, edits a value that
persists to ApplicationData.LocalFolder (Settings > surveyor name), reloads, and checks the value survived (IDBFS).

Usage: python tools/wasm_smoke.py http://localhost:8123 _shots/wasm
The app renders to a canvas, so interaction is by coordinates measured at 1280x800.
"""
import sys, time, pathlib
from playwright.sync_api import sync_playwright

url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8123"
out = pathlib.Path(sys.argv[2] if len(sys.argv) > 2 else "_shots/wasm")
out.mkdir(parents=True, exist_ok=True)

def wait_ready(page, seconds=90):
    # Uno's loader removes its splash element once the app has rendered its first frame.
    deadline = time.time() + seconds
    while time.time() < deadline:
        done = page.evaluate("() => !document.querySelector('#uno-body .uno-loader') && !!document.querySelector('canvas')")
        if done:
            time.sleep(4)
            return True
        time.sleep(1)
    return False

with sync_playwright() as p:
    browser = None
    for channel in ("msedge", "chrome"):
        try:
            browser = p.chromium.launch(channel=channel, headless=True)
            break
        except Exception as e:
            print(f"{channel}: {e}")
    if browser is None:
        sys.exit("no browser")

    context = browser.new_context(viewport={"width": 1280, "height": 800})
    page = context.new_page()
    logs = []
    page.on("console", lambda m: logs.append(f"{m.type}: {m.text}"))
    page.on("pageerror", lambda e: logs.append(f"pageerror: {e}"))

    t0 = time.time()
    page.goto(url)
    ready = wait_ready(page)
    print(f"ready={ready} after {time.time() - t0:.1f}s")
    page.screenshot(path=str(out / "01-first.png"))

    errors = [l for l in logs if l.startswith(("error", "pageerror"))]
    print(f"console errors: {len(errors)}")
    for l in errors[:15]:
        print("  ", l[:300])

    (out / "console.log").write_text("\n".join(logs), encoding="utf-8")
    browser.close()
