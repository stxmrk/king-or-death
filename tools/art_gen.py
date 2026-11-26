"""Generate UI key art via kie.ai GPT Image 2 (same API as the pulsar project).

    python tools/art_gen.py            # generates every missing file in JOBS
    python tools/art_gen.py --force    # regenerate all
    python tools/art_gen.py Icons/     # only jobs whose name starts with this prefix

Jobs under Icons/ are square 1K renders downscaled to 256 px; jobs under Ornament/ are gold-on-black 1K renders
whose black is keyed out to alpha and trimmed (both need Pillow).

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

ICON = (
    "Original dark fantasy RTS game interface icon, painterly, one bold centered subject readable at small size, "
    "rich detail, strong silhouette, muted palette of teal-black and weathered steel with warm gold rim light, "
    "dark vignette background filling the whole square edge to edge, no border, no frame, no text, no letters. "
)
PORTRAIT = (
    "Original dark fantasy RTS unit portrait, painterly head-and-shoulders bust, three-quarter view, dramatic warm "
    "gold key light, dark teal-black background filling the whole square, no border, no frame, no text. "
)
for name, subject in {
    "res_wood": "a small stack of freshly chopped pine logs",
    "res_stone": "three cut grey limestone blocks",
    "res_food": "a sheaf of golden wheat beside a round bread loaf",
    "cmd_build": "a carpenter's hammer crossed over a timber house frame",
    "cmd_recruit": "a sword standing point-down before a small teal banner",
    "cmd_crew": "a woodcutter's axe buried in a log stump",
    "cmd_raid": "a curved war horn bound in iron, red cloth tied to it",
    "cmd_bury": "a spade stuck in a fresh earth grave mound with a wooden marker",
    "cmd_burn": "a burning funeral pyre of logs, bright flames",
    "cmd_hide": "a heap of cut pine branches covering something on the forest floor",
    "cmd_carry": "a canvas stretcher with two wooden poles",
    "cmd_drop": "an empty canvas stretcher lying on the ground",
    "cmd_escort": "a round teal shield in front of a wooden wagon wheel",
    "cmd_route": "a wooden signpost at a fork of a dirt road",
    "cmd_halt": "a raised armored gauntlet, palm forward, halt gesture",
}.items():
    JOBS["Icons/" + name + ".png"] = ("1:1", ICON + "Subject: " + subject + ".")
for name, subject in {
    "unit_swordsman": "a young royal swordsman in a steel kettle helmet and a teal tabard over mail",
    "unit_worker": "a weathered frontier woodcutter in linen and leather, axe on shoulder",
    "unit_wagon": "a covered supply wagon with a canvas top pulled by a draft horse, front view",
    "unit_wolf": "a grey forest wolf with amber eyes, bared teeth",
    "unit_ashen": "a grim raider in a dark iron helmet, ash-grey cloak and rust-red tabard",
    "unit_store": "a fortified timber storehouse with a stone base and a teal pennant",
}.items():
    JOBS["Icons/" + name + ".png"] = ("1:1", PORTRAIT + "Subject: " + subject + ".")
ORNAMENT = (
    "Ornate antique gold metal filigree, polished gilded scrollwork with fine engraving, crisp edges, soft warm "
    "highlights, isolated on a pure flat black background, nothing else in the image, no text, no letters. "
)
JOBS["Ornament/emblem.png"] = ("1:1", ORNAMENT + "A heraldic emblem: a royal crown above a single downward-pointing "
                               "longsword, framed by a broken laurel wreath, symmetrical, centered.")
JOBS["Ornament/corner.png"] = ("1:1", ORNAMENT + "A single frame corner ornament in the top-left corner of the image: "
                               "two thin straight gilded lines meeting at a right angle along the top and left edges, "
                               "with a small elegant scroll flourish at the corner. The rest of the image is pure black.")
JOBS["Ornament/divider.png"] = ("16:9", ORNAMENT + "A thin horizontal divider ornament across the middle of the image: "
                                "a slim gilded line with a small diamond and scroll flourish at its center, "
                                "symmetrical, fading thin at both ends.")
JOBS["results_victory.png"] = ("16:9", STYLE + "Victory at a frontier river bridge at golden sunrise: a teal and gold "
                               "royal banner raised on the stone bridge, silhouettes of swordsmen with raised swords, "
                               "a supply wagon crossing, warm hopeful light breaking through clouds.")
JOBS["results_defeat.png"] = ("16:9", STYLE + "Defeat on the frontier at night: a fallen teal banner in the mud before "
                              "a burning wooden outpost, embers in the air, silhouettes of wolves on a ridge under a "
                              "cold moon, somber, no visible bodies or blood.")
JOBS["Texture/panel.png"] = ("1:1", "Seamless tileable texture of dark weathered leather over worn charcoal canvas, very subtle grain, "
                              "faint fine scratches, almost black with a cold teal-grey tint, very low contrast, perfectly flat "
                              "even lighting, no vignette, no objects, no pattern motifs, no text.")


def tileable(path: str, size: int) -> None:
    """Mirror the render into a 2x2 block (seamless by construction), shrink, and set its contrast for a panel fill."""
    from PIL import Image, ImageOps, ImageEnhance
    im = Image.open(path).convert("RGB").resize((size // 2, size // 2), Image.LANCZOS)
    out = Image.new("RGB", (size, size))
    out.paste(im, (0, 0))
    out.paste(ImageOps.mirror(im), (size // 2, 0))
    out.paste(ImageOps.flip(im), (0, size // 2))
    out.paste(ImageOps.flip(ImageOps.mirror(im)), (size // 2, size // 2))
    out = ImageEnhance.Contrast(out).enhance(1.25)
    out.save(path)


def key_black(path: str, width: int) -> None:
    """Turn gold-on-black into gold-on-transparent: alpha from brightness, colour un-premultiplied, then trimmed."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    px = im.load()
    out = Image.new("RGBA", im.size)
    op = out.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b = px[x, y]
            a = max(r, g, b)
            a = 0 if a < 24 else min(255, int((a - 24) * 255 / 200))
            op[x, y] = (min(255, r * 255 // max(a, 1)), min(255, g * 255 // max(a, 1)), min(255, b * 255 // max(a, 1)), a) if a else (0, 0, 0, 0)
    box = out.getbbox()
    if box:
        out = out.crop(box)
    out = out.resize((width, max(1, round(out.height * width / out.width))), Image.LANCZOS)
    out.save(path)


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
    prefix = next((a for a in sys.argv[1:] if not a.startswith("--")), "")
    pending = {}
    for name, (ratio, prompt) in JOBS.items():
        if name.startswith(prefix) and (force or not os.path.exists(os.path.join(OUT, name))):
            icon = name.startswith(("Icons/", "Ornament/", "Texture/"))
            body = {"model": "gpt-image-2-text-to-image",
                    "input": {"prompt": prompt, "aspect_ratio": ratio, "resolution": "1K" if icon else "2K"}}
            pending[call("POST", API + "/createTask", body)["taskId"]] = name
            time.sleep(1)
    deadline = time.time() + 900
    while pending and time.time() < deadline:
        time.sleep(5)
        for tid, name in list(pending.items()):
            info = call("GET", f"{API}/recordInfo?taskId={tid}")
            if info.get("state") == "success":
                path = os.path.join(OUT, name)
                os.makedirs(os.path.dirname(path), exist_ok=True)
                urllib.request.urlretrieve(json.loads(info["resultJson"])["resultUrls"][0], path)
                if name.startswith("Texture/"):
                    tileable(path, 512)
                elif name.startswith("Ornament/"):
                    key_black(path, {"emblem.png": 512, "corner.png": 192, "divider.png": 1024}[os.path.basename(name)])
                elif name.startswith("Icons/"):
                    from PIL import Image
                    Image.open(path).convert("RGB").resize((256, 256), Image.LANCZOS).save(path)
                print("ok", name, info.get("creditsConsumed", "?"), "cr")
                del pending[tid]
            elif info.get("state") == "fail":
                print("FAIL", name, info.get("failCode"), info.get("failMsg"))
                del pending[tid]
    for name in pending.values():
        print("TIMEOUT", name)


if __name__ == "__main__":
    main()
