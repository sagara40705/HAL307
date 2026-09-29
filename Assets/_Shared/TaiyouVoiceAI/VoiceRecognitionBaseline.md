# Voice Recognition Baseline (T0)

記録日: 2026-09-18  
対象: `Assets/_Shared/TaiyouVoiceAI/` と、Pause音声認識を保持する `Assets/_Shared/InGameUI/Menu/Scenes/Pause.unity`

この文書はT1変更直前の比較基準を記録する。設定値はC#フィールドの初期値ではなく、実行時に優先されるScene内のInspectorシリアライズ値を記載する。

## 1. T1変更前の共有マイク構造

### インスタンスごとの状態

`MicrophoneInput` の各インスタンスは次を保持していた。

- `microphoneClip`: 参加時点の共有`AudioClip`をローカルにキャッシュ
- `deviceName`: 選択したマイクの名前
- `sharedRecording`: 参加中の共有録音への参照
- `isUsingSharedRecording`: `UseCount`へ参加済みかを示すフラグ
- `sampleBuffer`: `sampleRate * recordLengthSec`分のPCM読取先

### static/shared状態

```text
static Dictionary<string, SharedRecording> SharedRecordings
  key: deviceName（完全一致）

SharedRecording
  AudioClip Clip
  int UseCount
```

同じ`deviceName`を選択したゲーム側とPause側は、同じDictionary要素を共有する。デバイス選択は`HyperX`または`SoloCast`を名前に含むデバイスを最優先し、次に外付け候補、最後に先頭デバイスを使用する。

## 2. T1変更前のライフサイクル

### Start / OnEnable

1. `Start()`が`sampleBuffer`を初期化する。
2. `StartRecordingWithRetry()`を開始し、0.5秒後から最大10回マイクを探索する。
3. `OnEnable()`時、バッファ初期化済みかつ共有録音へ未参加なら同じコルーチンを再開する。

独立した`StartRecording()`メソッドはなく、録音開始は`StartRecordingWithRetry()`から`AcquireSharedRecording()`を経由して`Microphone.Start()`を呼ぶ構成である。

### AcquireSharedRecording

1. `deviceName`をキーに`SharedRecordings`を検索する。
2. セッションまたは`Clip`が無ければ`Microphone.Start()`を呼び、新しい`SharedRecording`をDictionaryへ登録する。
3. 既存セッションがあれば再利用する。
4. `UseCount`を1増やす。
5. 共有`Clip`をインスタンスの`microphoneClip`へコピーする。

### GetLatestSamples

1. `RestartRecordingIfNeeded()`を呼ぶ。
2. インスタンスがキャッシュしている`microphoneClip`からPCMを読む。
3. `Microphone.GetPosition(deviceName)`から直近`sampleCount`分の開始位置を計算する。
4. `AudioClip.GetData()`で`sampleBuffer`へ読み込む。

### RestartRecordingIfNeeded

`Microphone.IsRecording(deviceName)`がfalseの場合、呼出元インスタンスだけが次の処理を行っていた。

1. 参加中セッションの`UseCount`を1減らす。
2. 自身の`microphoneClip`と`sharedRecording`をnullにする。
3. `SharedRecordings.Remove(deviceName)`で共有要素自体を削除する。
4. `AcquireSharedRecording()`で新しい`AudioClip`と共有要素を作る。

この処理では、他の利用者が残っていてもDictionary要素が置き換わる。他の利用者は古い`SharedRecording`と古い`microphoneClip`を保持し続ける可能性があり、再作成時に全体の`UseCount`も引き継がれない。

### OnDisable / ReleaseSharedRecording

1. `OnDisable()`が録音開始待機コルーチンを停止する。
2. `ReleaseSharedRecording()`が`UseCount`を1減らす。
3. `UseCount == 0`の場合だけ`Microphone.End(deviceName)`を呼び、Dictionaryから共有要素を削除する。
4. インスタンスの参照と参加フラグを解除する。

独立した`StopRecording()`メソッドはなく、停止処理は`OnDisable()`から`ReleaseSharedRecording()`を経由して行われる。

通常のDisableだけなら最後の利用者まで録音を維持できるが、上記Restart処理によるDictionary置換とは整合しない。

## 3. Scene設定値

### ゲーム側

Scene: `Assets/_Shared/TaiyouVoiceAI/Scenes/VoiceAi.unity`  
Component: `VoiceOnnxRunner`

| 項目 | Scene設定値 |
| --- | ---: |
| `inferenceInterval` | `0.4` |
| `minVolumeThreshold` | `0.001` |
| `minimumTopScore` | `1.0` |
| `minimumTop2Margin` | `1.0` |
| `muscleNoneMargin` | `2.0` |
| `powerNoneMargin` | `2.0` |
| `yaaNoneMargin` | `2.0` |

### Pause側

Scene: `Assets/_Shared/InGameUI/Menu/Scenes/Pause.unity`  
Component: `VoiceOnnxRunner_Menu`

| 項目 | Scene設定値 |
| --- | ---: |
| `inferenceInterval` | `0.7` |
| `minVolumeThreshold` | `0.0001` |
| `timeNoneMargin` | `1.5` |
| `timeTop2Margin` | `1.0` |
| `timeRequiredConsecutive` | `1` |

## 4. T1で守る比較条件

- Runner、MFCC、ONNX、認識閾値、コマンド処理、Scene構成は変更しない。
- 同じ`deviceName`の利用者は1つの共有録音セッションを使う。
- 再録音では利用者数を維持し、全利用者が次回PCM取得時に新しいClipを見る。
- `UseCount == 0`の場合だけ録音を終了する。
- 録音世代は再作成のたびに増加し、PCM読取時に現在世代を確認できるようにする。
