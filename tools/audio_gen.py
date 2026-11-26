"""Game audio for KING OR DEATH.

    python tools/audio_gen.py ui          # synthesize the UI click set (numpy, free, deterministic)
    python tools/audio_gen.py suno        # generate missing stingers, ambience and music via kie.ai Suno
    python tools/audio_gen.py suno --force
    python tools/audio_gen.py voice       # narrator and advisor lines, EN + RU (Gemini 3.8 Flash TTS)

Everything lands in Assets/_Game/Resources/Audio as .ogg, trimmed, faded and loudness-matched, so UiAudio can
Resources.Load it by name. Suno returns two takes per request; the first one is used and both are kept in
tools/.audio_raw for re-picking. Key: same as tools/art_gen.py (never printed).
"""
import json
import os
import sys
import time
import urllib.error
import urllib.request

import numpy as np
import soundfile as sf

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import art_gen  # noqa: E402  (key handling and the jobs API wrapper)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "_Game", "Resources", "Audio")
RAW = os.path.join(ROOT, "tools", ".audio_raw")
API = "https://api.kie.ai/api/v1/jobs"
RATE = 44100

NO_MUSIC = " Sound effect only, no melody, no music, no vocals."
# name: (kind, prompt or style, max seconds, loop)
SUNO = {
    "sting_raid": ("sound", "A single long medieval war horn blast in the distance, ominous, echoing across a misty valley." + NO_MUSIC, 6, False),
    "sting_wolves": ("sound", "A pack of wolves howling together in a dark pine forest at night, distant and eerie." + NO_MUSIC, 6, False),
    "sting_capture": ("sound", "A short triumphant medieval brass fanfare ending on a single timpani hit, heroic, about three seconds, instrumental.", 5, False),
    "sting_lost": ("sound", "One deep ominous war drum hit followed by a low dark brass swell, foreboding, about three seconds, instrumental.", 5, False),
    "sting_delivery": ("sound", "A wooden cart rolling to a stop on a dirt road, a horse snorts, a sack of coins jingles." + NO_MUSIC, 4, False),
    "sting_wagon": ("sound", "A wooden wagon smashing and splintering, crates breaking apart, a single violent impact." + NO_MUSIC, 4, False),
    "sting_build": ("sound", "A carpenter hammering wooden beams: three firm hammer knocks on timber, outdoors." + NO_MUSIC, 3, False),
    "sting_victory": ("sound", "A triumphant orchestral medieval victory fanfare with brass, choir and timpani, about six seconds, ending on a resolved major chord.", 9, False),
    "sting_defeat": ("sound", "A somber orchestral defeat sting with low choir and cello, a slow descending melody fading out, about six seconds.", 9, False),
    "amb_frontier": ("sound", "Calm outdoor daytime ambience: wind over a meadow, distant birds, a river flowing, leaves rustling. Continuous background." + NO_MUSIC, 40, True),
    "music_menu": ("music", "dark fantasy orchestral main menu theme, slow and melancholic, solo cello, low male choir, distant war drums, harp, "
                            "medieval, epic but restrained, cinematic, instrumental", 150, True),
    "music_match": ("music", "calm medieval fantasy strategy game background music, lute, warm strings, soft flute, folk, gentle and unobtrusive, "
                             "instrumental, no percussion build-ups", 180, True),
}


# Narrator / advisor voice. ElevenLabs v3 via kie.ai (Gemini 3.8 Flash TTS timed out on every task, 2026-11-26).
# Voice ids are ElevenLabs premade voices: Brian (deep narrator) and Callum (husky, intense advisor).
SPEAKERS = {"brief": ("nPczCjzI2devNBz1zQrb", "[solemn] "), "advisor": ("N2lVS1w4EtoT3dr4eOWO", "[urgent] ")}
VOICE = "Algenib"
STYLES = {
    "brief": {"en": "A grave, weathered royal herald reading a dark fantasy war chronicle: slow, low, solemn, with weight "
                    "and short dramatic pauses.",
              "ru": "Суровый немолодой королевский глашатай читает хронику войны в мрачном фэнтези: медленно, низким "
                    "голосом, торжественно, с весом и короткими паузами."},
    "advisor": {"en": "A seasoned military advisor reporting from the battlefield: urgent but controlled, low voice, short "
                      "and clear.",
                "ru": "Опытный военный советник докладывает с поля боя: собранно и тревожно, низким голосом, коротко и чётко."},
}
LINES = {
    "briefing": ("brief",
                 "The First Frontier. The Crown has sent you to the very edge of its lands, where the Silverrun divides "
                 "order from ash. Hold the bridge. Take the frontier posts and keep Crownwatch supplied. And remember: this "
                 "land remembers every battle. The fallen stay where they fall, and the forest answers their scent.",
                 "Первый рубеж. Корона послала вас на самый край своих земель — туда, где Серебрянка отделяет порядок от "
                 "пепла. Удержите мост. Захватите пограничные посты и наладьте снабжение Коронного Дозора. И помните: этот "
                 "край помнит каждую битву. Павшие остаются там, где пали, — и лес отвечает на их запах."),
    "raid": ("advisor", "Raiders! The Ashen march on the bridge.", "Набег! Пепельные идут к мосту."),
    "wolves": ("advisor", "The wolves have left their den. They follow the scent of the fallen.",
               "Волки вышли из логова. Они идут на запах павших."),
    "captured": ("advisor", "The post is ours. Its stores now serve the Crown.",
                 "Пост наш. Его запасы теперь служат Короне."),
    "lost": ("advisor", "We have lost a post.", "Мы потеряли пост."),
    "delivery": ("advisor", "The caravan has arrived. The keep is supplied.", "Обоз дошёл. Крепость получила груз."),
    "wagon": ("advisor", "The caravan is destroyed! Salvage what you can.", "Обоз уничтожен! Спасайте что осталось."),
    "attack": ("advisor", "Our men are under attack!", "Наших людей атакуют!"),
    "victory": ("brief", "The frontier holds. The Crown will not forget your service.",
                "Рубеж устоял. Корона не забудет вашей службы."),
    "defeat": ("brief", "The frontier has fallen. The land will remember this too.", "Рубеж пал. Земля запомнит и это."),
}


def call(method, url, body=None, tries=6):
    for attempt in range(tries):
        try:
            return art_gen.call(method, url, body)
        except (urllib.error.URLError, TimeoutError, ConnectionError) as e:
            if attempt == tries - 1:
                raise
            print("  network hiccup, retrying:", getattr(e, "reason", e))
            time.sleep(3 + attempt * 2)


def finish(audio, max_seconds, loop, peak_db=-1.0, rms_db=-18.0, mono=True):
    """Trim silence, cap length with a fade, match loudness; loops get a crossfaded seam. Keeps stereo unless mono."""
    audio = np.asarray(audio, dtype=np.float64)
    if audio.ndim == 1:
        audio = audio[:, None]
    if mono:
        audio = audio.mean(axis=1, keepdims=True)
    level = np.abs(audio).max(axis=1)
    loud = np.where(level > 10 ** (-48 / 20))[0]
    if len(loud):
        audio = audio[max(0, loud[0] - int(.01 * RATE)): loud[-1] + int(.05 * RATE)]
    cap = int(max_seconds * RATE)
    if len(audio) > cap:
        audio = audio[:cap].copy()
        if not loop:
            fade = min(len(audio), int(.6 * RATE))
            audio[-fade:] *= (np.linspace(1, 0, fade) ** 2)[:, None]
    if loop:
        # fold the tail over the head so the seam is a crossfade, not a click
        x = min(int(1.5 * RATE), len(audio) // 4)
        head, tail = audio[:x].copy(), audio[-x:].copy()
        ramp = np.linspace(0, 1, x)[:, None]
        audio = audio[:-x].copy()
        audio[:x] = head * ramp + tail * (1 - ramp)
    else:
        n = int(.005 * RATE)
        audio[:n] *= np.linspace(0, 1, n)[:, None]
    rms = np.sqrt(np.mean(audio ** 2)) + 1e-9
    audio = audio * (10 ** (rms_db / 20) / rms)
    peak = np.max(np.abs(audio)) + 1e-9
    limit = 10 ** (peak_db / 20)
    if peak > limit:
        audio = audio * (limit / peak)
    return (audio[:, 0] if audio.shape[1] == 1 else audio).astype(np.float32)


def write(name, audio):
    # libsndfile's Vorbis encoder fails on long single writes (empty file or crash), so feed it in blocks
    os.makedirs(OUT, exist_ok=True)
    channels = 1 if audio.ndim == 1 else audio.shape[1]
    with sf.SoundFile(os.path.join(OUT, name + ".ogg"), "w", RATE, channels, format="OGG", subtype="VORBIS") as f:
        for start in range(0, len(audio), 16384):
            f.write(audio[start:start + 16384])


# ---------- procedural UI set ----------

def env(n, attack, decay):
    t = np.arange(n) / RATE
    return np.minimum(1, t / max(attack, 1e-4)) * np.exp(-t / decay)


def tone(freq, seconds, decay, attack=.002, harmonics=((1, 1.0),)):
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    wave = sum(a * np.sin(2 * np.pi * freq * h * t) for h, a in harmonics)
    return wave * env(n, attack, decay)


def noise(seconds, decay, lo=None, hi=None, seed=1, attack=.001):
    n = int(seconds * RATE)
    rng = np.random.default_rng(seed)
    x = rng.standard_normal(n)
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(n, 1 / RATE)
    if lo:
        spec[f < lo] = 0
    if hi:
        spec[f > hi] = 0
    x = np.fft.irfft(spec, n)
    return x / (np.max(np.abs(x)) + 1e-9) * env(n, attack, decay)


def pad(*parts):
    n = max(len(p) for p in parts)
    return sum(np.pad(p, (0, n - len(p))) for p in parts)


def ui_set():
    sounds = {
        # soft wooden tick for hovering a button
        "ui_hover": (pad(tone(2400, .05, .008, harmonics=((1, 1), (1.5, .3))) * .5, noise(.05, .004, 3000, 9000, 2) * .25), -26),
        # dry latch click
        "ui_click": (pad(tone(1250, .09, .018, harmonics=((1, 1), (2.01, .45), (3.2, .2))), noise(.06, .006, 1500, 8000, 3) * .6), -19),
        # tab / small toggle
        "ui_tab": (pad(tone(1650, .07, .014, harmonics=((1, 1), (2.5, .3))), noise(.05, .005, 2000, 9000, 4) * .4), -21),
        # back: soft downward swish
        "ui_back": (noise(.16, .05, 600, 3500, 5, attack=.03) * np.linspace(1, .2, int(.16 * RATE)), -22),
        # confirm: two-note bell, a fifth apart
        "ui_confirm": (pad(tone(880, .55, .22, harmonics=((1, 1), (2, .35), (3, .12))),
                           np.pad(tone(1320, .45, .2, harmonics=((1, 1), (2, .3))), (int(.07 * RATE), 0)) * .8), -18),
        # error: dull double knock
        "ui_error": (pad(tone(190, .12, .03, harmonics=((1, 1), (2, .5))),
                         np.pad(tone(170, .12, .03, harmonics=((1, 1), (2, .5))), (int(.09 * RATE), 0))), -17),
        # panel open: parchment unroll
        "ui_open": (noise(.32, .12, 800, 6000, 6, attack=.06), -23),
        # alert ping for red log entries
        "ui_alert": (pad(tone(660, .5, .16, harmonics=((1, 1), (2.76, .25))),
                         np.pad(tone(523, .5, .18, harmonics=((1, 1), (2.76, .25))), (int(.12 * RATE), 0))), -18),
    }
    for name, (audio, rms) in sounds.items():
        write(name, finish(audio.astype(np.float64), 2, False, rms_db=rms))
        print("ok", name)


# ---------- Suno ----------

def suno_body(kind, prompt, seconds, loop):
    if kind == "sound":
        return {"model": "ai-music-api/sounds", "input": {"prompt": prompt, "model": "V6", "sound_loop": loop}}
    return {"model": "ai-music-api/generate",
            "input": {"custom_mode": True, "instrumental": True, "model": "V6", "title": "King or Death",
                      "style": prompt, "duration": seconds}}


def clips(info):
    resp = info.get("response")
    if isinstance(resp, str):
        resp = json.loads(resp)
    data = (resp or {}).get("data") or []
    urls = [c.get("audio_url") for c in data if c.get("audio_url")]
    if not urls and info.get("resultJson"):
        urls = json.loads(info["resultJson"]).get("resultUrls") or []
    return urls


def process(name, take=0):
    kind, _, seconds, loop = SUNO[name]
    audio, rate = sf.read(os.path.join(RAW, f"{name}.{take}.mp3"), dtype="float64", always_2d=True)
    if rate != RATE:
        t = np.arange(int(len(audio) * RATE / rate)) * rate / RATE
        audio = np.stack([np.interp(t, np.arange(len(audio)), audio[:, c]) for c in range(audio.shape[1])], 1)
    write(name, finish(audio, seconds, loop, rms_db=-20 if kind == "music" or loop else -16, mono=kind != "music"))


def suno(force):
    os.makedirs(RAW, exist_ok=True)
    pending = {}
    for name, (kind, prompt, seconds, loop) in SUNO.items():
        if force or not os.path.exists(os.path.join(OUT, name + ".ogg")):
            pending[call("POST", API + "/createTask", suno_body(kind, prompt, seconds, loop))["taskId"]] = name
            print("queued", name)
            time.sleep(1.5)
    deadline = time.time() + 1500
    while pending and time.time() < deadline:
        time.sleep(8)
        for tid, name in list(pending.items()):
            info = call("GET", f"{API}/recordInfo?taskId={tid}")
            state = info.get("state")
            urls = clips(info)
            if state == "fail":
                print("FAIL", name, info.get("failCode"), info.get("failMsg"))
                del pending[tid]
            elif state == "success" and urls:
                kind, _, seconds, loop = SUNO[name]
                for i, url in enumerate(urls[:2]):
                    raw = os.path.join(RAW, f"{name}.{i}.mp3")
                    urllib.request.urlretrieve(url, raw)
                process(name)
                print("ok", name, info.get("creditsConsumed", "?"), "cr")
                del pending[tid]
    for name in pending.values():
        print("TIMEOUT", name)


def voice(force, only=None, batch=4):
    """Generates vo_<line>_<lang>.ogg for every line in LINES, a few tasks at a time (many at once time out)."""
    os.makedirs(RAW, exist_ok=True)
    todo = [(line, lang, text, style) for line, (style, en, ru) in LINES.items() for lang, text in (("en", en), ("ru", ru))
            if (not only or line in only) and (force or not os.path.exists(os.path.join(OUT, f"vo_{line}_{lang}.ogg")))]
    for start in range(0, len(todo), batch):
        voice_batch(todo[start:start + batch])


def voice_batch(todo):
    pending = {}
    for line, lang, text, style in todo:
            name = f"vo_{line}_{lang}"
            if True:
                voice_id, tags = SPEAKERS[style]
                body = {"model": "elevenlabs/text-to-dialogue-v3", "input": {
                    "dialogue": [{"text": tags + text, "voice": voice_id}],
                    "stability": 0.5, "language_code": lang}}
                pending[call("POST", API + "/createTask", body)["taskId"]] = name
                time.sleep(1.2)
    print("queued", len(pending))
    deadline = time.time() + 900
    while pending and time.time() < deadline:
        time.sleep(4)
        for tid, name in list(pending.items()):
            info = call("GET", f"{API}/recordInfo?taskId={tid}")
            urls = clips(info)
            if info.get("state") == "fail":
                print("FAIL", name, info.get("failCode"), info.get("failMsg"))
                del pending[tid]
            elif info.get("state") == "success" and urls:
                ext = os.path.splitext(urls[0].split("?")[0])[1] or ".wav"
                raw = os.path.join(RAW, name + ext)
                urllib.request.urlretrieve(urls[0], raw)
                audio, rate = sf.read(raw, dtype="float64", always_2d=True)
                if rate != RATE:
                    t = np.arange(int(len(audio) * RATE / rate)) * rate / RATE
                    audio = np.stack([np.interp(t, np.arange(len(audio)), audio[:, c]) for c in range(audio.shape[1])], 1)
                write(name, finish(audio, 60, False, rms_db=-17))
                print("ok", name, info.get("creditsConsumed", "?"), "cr")
                del pending[tid]
    for name in pending.values():
        print("TIMEOUT", name)


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "ui"
    if what == "ui":
        ui_set()
    elif what == "voice":
        voice("--force" in sys.argv, [a for a in sys.argv[2:] if not a.startswith("--")])
    elif what == "raw":  # rebuild .ogg from downloaded takes: raw name[:take] ...
        for arg in sys.argv[2:]:
            n, _, t = arg.partition(":")
            process(n, int(t or 0))
            print("ok", arg)
    else:
        suno("--force" in sys.argv)
