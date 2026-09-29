import argparse
import sys
import time
from pathlib import Path

import numpy as np
import sounddevice as sd
import soundfile as sf


LABELS = ["ue", "sita", "hidari", "migi", "kettei", "time", "none"]

PROMPTS = {
    "ue": "うえ",
    "sita": "した",
    "hidari": "ひだり",
    "migi": "みぎ",
    "kettei": "けってい",
    "time": "タイム",
    "none": "無音・環境音・別の言葉",
}

SAMPLE_RATE = 16000
RECORD_SECONDS = 1.0


def list_devices():
    devices = sd.query_devices()
    print("Input devices:")
    for index, device in enumerate(devices):
        if int(device.get("max_input_channels", 0)) <= 0:
            continue

        default_mark = " (default)" if index == sd.default.device[0] else ""
        print(f"  {index}: {device['name']}{default_mark}")


def select_input_device(preferred_device):
    devices = sd.query_devices()
    input_devices = [
        (index, device)
        for index, device in enumerate(devices)
        if int(device.get("max_input_channels", 0)) > 0
    ]

    if not input_devices:
        raise RuntimeError("入力マイクが見つかりません。")

    if preferred_device is not None:
        if preferred_device.isdigit():
            index = int(preferred_device)
            sd.query_devices(index, "input")
            return index

        preferred_lower = preferred_device.lower()
        for index, device in input_devices:
            if preferred_lower in str(device["name"]).lower():
                return index

        raise RuntimeError(f"指定したマイクが見つかりません: {preferred_device}")

    preferred_keywords = ["hyperx", "solocast", "usb", "external"]
    for keyword in preferred_keywords:
        for index, device in input_devices:
            if keyword in str(device["name"]).lower():
                return index

    non_realtek = [
        (index, device)
        for index, device in input_devices
        if "realtek" not in str(device["name"]).lower()
    ]
    if non_realtek:
        return non_realtek[0][0]

    return input_devices[0][0]


def next_index(label_dir, label):
    max_index = 0
    for wav in label_dir.glob(f"{label}_*.wav"):
        stem = wav.stem
        suffix = stem.rsplit("_", 1)[-1]
        if suffix.isdigit():
            max_index = max(max_index, int(suffix))

    return max_index + 1


def count_wavs(label_dir):
    return len(list(label_dir.glob("*.wav")))


def record_one(device, seconds, sample_rate):
    frames = int(seconds * sample_rate)
    audio = sd.rec(
        frames,
        samplerate=sample_rate,
        channels=1,
        dtype="float32",
        device=device,
    )
    sd.wait()
    return np.asarray(audio, dtype=np.float32)


def countdown(seconds):
    for remaining in range(seconds, 0, -1):
        print(f"  {remaining}...", flush=True)
        time.sleep(1.0)


def record_label(label, label_dir, amount, args, device):
    label_dir.mkdir(parents=True, exist_ok=True)
    start_index = next_index(label_dir, label)
    prompt = PROMPTS[label]

    print()
    print("=" * 60)
    print(f"Label: {label}")
    print(f"Prompt: {prompt}")
    print(f"Save dir: {label_dir}")
    print(f"Recording count: {amount}")
    print("=" * 60)

    if amount <= 0:
        print("既に目標数に達しています。スキップします。")
        return

    input("Enterでこのラベルの自動録音を開始します > ")

    for offset in range(amount):
        index = start_index + offset
        filename = label_dir / f"{label}_{index:04d}.wav"

        print()
        print(f"[{label}] {offset + 1}/{amount} -> {filename.name}")
        if label == "none":
            print("  何も言わない、または環境音を入れてください。")
        else:
            print(f"  「{prompt}」と発声してください。")

        if args.countdown > 0:
            countdown(args.countdown)

        print("  REC", flush=True)
        audio = record_one(device, args.seconds, args.sample_rate)
        sf.write(filename, audio, args.sample_rate)

        max_abs = float(np.max(np.abs(audio))) if audio.size > 0 else 0.0
        rms = float(np.sqrt(np.mean(audio * audio))) if audio.size > 0 else 0.0
        print(f"  saved maxAbs={max_abs:.6f} rms={rms:.6f}")

        if args.interval > 0:
            time.sleep(args.interval)


def parse_args():
    parser = argparse.ArgumentParser(
        description="Pause menu voice dataset recorder. Records ue/sita/hidari/migi/kettei/time/none efficiently."
    )
    parser.add_argument(
        "--output",
        default="dataset",
        help="dataset folder. Example: C:\\HAL\\python\\voice_ai_Menu_Controller\\dataset",
    )
    parser.add_argument(
        "--labels",
        nargs="*",
        default=LABELS,
        help="labels to record. Default: all labels",
    )
    parser.add_argument(
        "--target",
        type=int,
        default=100,
        help="target total wav count per label. Default: 100",
    )
    parser.add_argument(
        "--add",
        type=int,
        default=None,
        help="record this many additional wavs per label instead of filling to --target",
    )
    parser.add_argument(
        "--device",
        default=None,
        help="input device index or part of device name. Default: auto select external mic when possible",
    )
    parser.add_argument(
        "--list-devices",
        action="store_true",
        help="show input devices and exit",
    )
    parser.add_argument("--sample-rate", type=int, default=SAMPLE_RATE)
    parser.add_argument("--seconds", type=float, default=RECORD_SECONDS)
    parser.add_argument("--countdown", type=int, default=1)
    parser.add_argument("--interval", type=float, default=0.25)
    return parser.parse_args()


def main():
    args = parse_args()

    if args.list_devices:
        list_devices()
        return 0

    invalid = [label for label in args.labels if label not in LABELS]
    if invalid:
        print("Invalid labels:", invalid)
        print("Available labels:", ", ".join(LABELS))
        return 1

    output_dir = Path(args.output)
    device = select_input_device(args.device)
    device_info = sd.query_devices(device, "input")

    print("Selected input device:")
    print(f"  {device}: {device_info['name']}")
    print()
    print("Recording settings:")
    print(f"  output={output_dir}")
    print(f"  labels={', '.join(args.labels)}")
    print(f"  sampleRate={args.sample_rate}")
    print(f"  seconds={args.seconds}")
    if args.add is None:
        print(f"  mode=fill to target, target={args.target} per label")
    else:
        print(f"  mode=add, add={args.add} per label")

    print()
    print("Current counts:")
    plan = []
    for label in args.labels:
        label_dir = output_dir / label
        existing = count_wavs(label_dir) if label_dir.exists() else 0
        amount = args.add if args.add is not None else max(0, args.target - existing)
        plan.append((label, label_dir, amount, existing))
        print(f"  {label}: existing={existing}, record={amount}")

    print()
    input("Enterで録音計画を開始します。Ctrl+Cでいつでも停止できます > ")

    try:
        for label, label_dir, amount, _ in plan:
            record_label(label, label_dir, amount, args, device)
    except KeyboardInterrupt:
        print()
        print("録音を中断しました。再実行すると続きから録音できます。")
        return 130

    print()
    print("録音完了。最終カウント:")
    for label in args.labels:
        label_dir = output_dir / label
        existing = count_wavs(label_dir) if label_dir.exists() else 0
        print(f"  {label}: {existing}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
