"""Generate UI key art via kie.ai GPT Image 2 (same API as the pulsar project).

    python tools/art_gen.py            # generates every missing file in JOBS
    python tools/art_gen.py --force    # regenerate all

Key: KIE_API_KEY env var or F:/LocalProjectsLLM/pulsar/tools/voices/.kie_key. Never printed.
"""
import json
import os
import sys
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "UI")
API = "https://api.kie.ai/api/v1/jobs"
KEY_FILE = r"F:\LocalProjectsLLM\pulsar\tools\voices\.kie_key"

STYLE = (
    "Original dark fantasy strategy game key art, painterly matte painting, muted desaturated palette "
    "of deep teal-black shadows, cold mist and warm muted gold highlights, low sun, cinematic composition, "
    "no text, no letters, no logos, no watermark, no UI. "
)
JOBS = {
    "menu_bg.png": ("16:9", STYLE + "A lone grey stone fortress keep with one gold banner on a hill above a dark pine "
                    "forest and a misty river with a single wooden bridge, dawn. Fortress sits in the right third; "
                    "the left 40% of the frame is calm dark sky and dark hills, empty space for a menu."),
    "scenario_frontier.png": ("16:9", STYLE + "A frontier wooden outpost with a capture flag at the edge of a pine forest, "
                              "a horse-drawn supply wagon on a dirt road, a small escort of swordsmen, distant river."),
    "loading_bg.png": ("16:9", STYLE + "Aftermath of a small battle near a river bridge at dusk: fallen shields and a "
                       "burial mound in the foreground, crows, wolves' eyes glinting in the dark forest edge. "
                       "Somber, not gory, no visible bodies or blood."),
}


def key() -> str:
    k = os.environ.get("KIE_API_KEY", "").strip()
    if not k and os.path.exists(KEY_FILE):
        k = open(KEY_FILE, encoding="utf-8").read().strip()
    if not k:
        sys.exit("No kie.ai key: set KIE_API_KEY")
    return k


def call(method: str, url: str, body=None) -> dict:
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", "Bearer " + key())
    req.add_header("Content-Type", "application/json")
    with urllib.request.urlopen(req, timeout=120) as r:
        resp = json.loads(r.read())
    if int(resp.get("code", 0)) != 200:
        raise RuntimeError(f"kie.ai {resp.get('code')}: {resp.get('msg')}")
    return resp.get("data") or {}


def main() -> None:
    os.makedirs(OUT, exist_ok=True)
    force = "--force" in sys.argv
    pending = {}
    for name, (ratio, prompt) in JOBS.items():
        if force or not os.path.exists(os.path.join(OUT, name)):
            body = {"model": "gpt-image-2-text-to-image",
                    "input": {"prompt": prompt, "aspect_ratio": ratio, "resolution": "2K"}}
            pending[call("POST", API + "/createTask", body)["taskId"]] = name
            time.sleep(1)
    deadline = time.time() + 900
    while pending and time.time() < deadline:
        time.sleep(5)
        for tid, name in list(pending.items()):
            info = call("GET", f"{API}/recordInfo?taskId={tid}")
            if info.get("state") == "success":
                urllib.request.urlretrieve(json.loads(info["resultJson"])["resultUrls"][0], os.path.join(OUT, name))
                print("ok", name, info.get("creditsConsumed", "?"), "cr")
                del pending[tid]
            elif info.get("state") == "fail":
                print("FAIL", name, info.get("failCode"), info.get("failMsg"))
                del pending[tid]
    for name in pending.values():
        print("TIMEOUT", name)


if __name__ == "__main__":
    main()
