# 音声認識システム調査レポート

調査日: 2026-09-15  
対象: Unityプロジェクト `HAL307` の現在の作業ツリー  
Unity: 6000.3.7f1  
Unity Inference Engine: 2.4.1

## 1. 概要

現在の音声認識は、1つのマイク録音を共有しながら、ゲーム用とPauseメニュー用で別々のMFCC前処理とONNXモデルを使用する構成になっている。

### 外部レビュー時の前提

このプロジェクトでは、1つのゲーム画面を1つのUnity Sceneだけで構成していない。`Prototype`を親となる進行Sceneとして開き、Player、敵、ポーズ判定、音声認識、UIなどを別SceneからAdditiveロードして組み合わせる。

そのため、レビュー時は次の点を前提にする必要がある。

- `VoiceAi.unity`だけを単独で見ても、実行時のPlayer参照は存在しない
- Sceneをまたぐ参照は、原則として`ContextManager`とContext名で解決する
- Pause Sceneはメニューを閉じてもロードされたままで、音声待ち受け用`VOICE`もActiveのままにする
- ゲーム用とPause用の音声認識は、同じ録音を読むが、MFCC、モデル、ラベル、フィルタ、実行先は別系統である
- SceneファイルのInspector値はC#のフィールド初期値を上書きするため、実行値の確認にはScene YAMLも必要である

```text
ゲーム用
MicrophoneInput
  -> MfccFeatureExtractor
  -> VoiceOnnxRunner
  -> VoiceCommandAction
  -> VoiceAiContext
  -> PlayerContext
  -> PlayerPoseContoroller.TryOnPose()

Pauseメニュー用
MicrophoneInput
  -> PythonCompatMfccFeatureExtractor
  -> VoiceOnnxRunner_Menu
  -> MenuVoiceCommandAction
  -> VoiceMainMenuController
```

ゲーム用とPause用にはそれぞれ`MicrophoneInput`が存在するが、同じデバイス名を選択した場合は、静的Dictionaryを通して同じ録音用`AudioClip`を共有する。

## 2. シーン構成

`GamePhaseSceneSettings`のフェーズ対応は次のとおり。

| GamePhase | Scene |
|---|---|
| Title | `Title` |
| InGame | `Prototype` |
| Result | `Result_.1` |
| Pause | `Pause` |

`Prototype.unity`は開始時に次のSceneをAdditiveロードする。

- `StageModel`
- `PoseParryJudge`
- `Enemy`
- `Player`
- `VoiceAi`
- `InGameUI`
- `AudienceRequest`
- `InGameState`

`PauseManager`は`Pause` SceneをAdditiveで事前ロードする。Pauseを閉じてもScene自体はUnloadせず、`VoiceMainMenuController.CloseMenu()`でメインメニューUIだけを非表示にする。

このため、通常ゲーム中もPause Sceneの`VOICE` GameObjectはActiveであり、Pause用音声認識は`time`を待ち受ける。通常ゲーム中はゲーム用とPause用の2つのRunnerが並行して動作する。

## 3. マイク入力と共有録音

対象ファイル: `Scripts/MicrophoneInput.cs`

### デバイス選択

`Microphone.devices`を列挙し、次の順番でデバイスを選択する。

1. 名前に`HyperX`または`SoloCast`を含むデバイス
2. `USB`、`External`、`Headset`、`Mic`、`マイク`などを含む外付け候補
3. `Microphone.devices[0]`

`Realtek`を含むデバイスは外付け候補としての優先度を下げる。ただし、Unityが切断済みデバイスを`Microphone.devices`に残している場合、その名前だけで選択してしまう可能性がある。現在は録音開始後の信号レベルを使った自動フォールバックは行っていない。

### 共有録音

デバイス名をキーに、次の情報を共有する。

```text
Dictionary<string, SharedRecording>
SharedRecording.Clip
SharedRecording.UseCount
```

録音設定は以下。

- sample rate: 16000 Hz
- recording length: 1秒
- loop: true
- sample count: 16000

参加時に`UseCount`を増やし、`OnDisable()`で減らす。最後の利用者が離れ、`UseCount == 0`になった場合だけ`Microphone.End(deviceName)`を呼ぶ。

### サンプル取得

`GetLatestSamples()`は次の順番で16000サンプルを取得する。

```text
Microphone.GetPosition(deviceName)
  -> startPosition = currentPosition - 16000
  -> マイナスならAudioClip末尾へwrap
  -> AudioClip.GetData(sampleBuffer, startPosition)
```

`sampleBuffer`は各`MicrophoneInput`インスタンスが個別に持つため、共有しているのは録音用`AudioClip`であり、返却配列そのものではない。

## 4. ゲーム用音声認識

### Scene構成

`VoiceAi.unity`の`VoiceAIManager`には次のComponentが設定されている。

- `MicrophoneInput`
- `MfccFeatureExtractor`
- `VoiceOnnxRunner`
- `VoiceCommandAction`
- `VoiceTextRecognizer`
- `VoiceAIManagerActiveTrace`

### ONNX

モデル: `Models/voice_command_model.onnx`

ONNXファイルを直接確認したShapeは次のとおり。

```text
input:  [1, 1, 20, 32]
output: [1, 4]
```

ラベルは`VoiceOnnxRunner.cs`にハードコードされている。

| index | label |
|---:|---|
| 0 | muscle |
| 1 | none |
| 2 | power |
| 3 | yaa |

ONNXファイルにはラベルメタデータが含まれていないため、学習時のクラス順とコード上の配列が一致していることが前提になる。

### MFCC

`MfccFeatureExtractor`の主な処理は次のとおり。

1. Pre-emphasis
2. Hamming window
3. FFTによるpower spectrum
4. Mel filter bank
5. 自然対数
6. DCT
7. MFCC行ごとのmean/std正規化
8. 時間軸を64へ線形補間

`VoiceAi.unity`上の設定値は以下。

- sample rate: 16000
- FFT size: 256
- hop length: 256
- Mel filters: 24
- pre-emphasis: 0.97
- normalize per feature: true

`MfccFeatureExtractor`は24x64を返すが、`VoiceOnnxRunner`はその左上20x32だけをONNX入力へコピーしている。ONNXのShapeには合うが、24x64生成後の切り出しという特徴量設計は不自然であり、学習時前処理との一致確認が必要である。

### 推論ステート

```text
Idle
  -> CheckVolume
  -> MakeFeature
  -> RunOnnx
  -> HandleResult
  -> Idle
```

Scene上の推論設定値は以下。

- inference interval: 0.4秒
- minimum volume: 0.001
- minimum top score: 1.0
- minimum top1/top2 margin: 1.0
- muscle/none margin: 2.0
- power/none margin: 2.0
- yaa/none margin: 2.0

音量は16000サンプル中の最大絶対値で判定する。安全フィルタ通過後、`none`以外を`VoiceCommandAction.ExecuteCommand()`へ渡す。

### ポーズ接続

コマンド対応は次のとおり。

| command | PoseID |
|---|---|
| power | `PoseID.Pose_Power` |
| yaa | `PoseID.Pose_Yaa` |
| muscle | `PoseID.Pose_Muscle` |
| none | 実行しない |

`VoiceCommandAction`は`VoiceAiContext.GetPlayerContext()`から`PlayerContext`を取得し、さらに`GetPlayerPoseContoroller`から`PlayerPoseContoroller`を取得する。初期取得に失敗しても、コマンド実行時に再度解決する。

`PlayerPoseContoroller.TryOnPose()`では、演出ロック中またはすでにポーズ中の場合は実行しない。実行時はポーズ判定、色、Animator Trigger、`OnPoseStart`イベントを更新する。

## 5. Pauseメニュー用音声認識

### Scene構成

`Pause.unity`の`VOICE` rootには次のComponentが設定されている。

- `VoiceMainMenuController`
- `MenuVoiceCommandAction`
- `VoiceOnnxRunner_Menu`
- `MicrophoneInput`
- `MfccFeatureExtractor`（現在はRunnerから未使用）
- `PythonCompatMfccFeatureExtractor`

### ONNXとラベル

モデル: `Models/voice_menuv4_model.onnx`  
ラベル: `Models/labels4.txt`

実際のONNX Shapeは次のとおり。

```text
input:  [1, 1, 24, 64]
output: [1, 7]
```

ラベル順は`labels4.txt`、コードのfallbackともに一致している。

| index | label | 操作 |
|---:|---|---|
| 0 | ue | 上 |
| 1 | sita | 下 |
| 2 | hidari | 左 |
| 3 | migi | 右 |
| 4 | kettei | 決定 |
| 5 | time | Pause開始・受付準備 |
| 6 | none | 無視 |

### Python互換MFCC

`PythonCompatMfccFeatureExtractor`はPause専用で、ゲーム用MFCCには影響しない。

- sample rate: 16000
- input samples: 16000へpad/trim
- n_mfcc: 24
- n_fft: 256
- hop length: 256
- n_mels: 128
- Hann window
- center相当の前後128サンプルpadding
- power spectrum
- Slaney mel scale / Slaney normalization
- 10 x log10、amin=1e-10、top_db=80
- DCT-II、ortho normalization
- 63フレーム生成後に64へ0 padding
- 24x64全体でglobal mean/std正規化

通常はCSV出力OFFで、Inspectorから次回抽出時だけ24x64をCSV出力できる。

### 推論とフィルタ

`Pause.unity`上の設定値は以下。

- inference interval: 0.7秒
- minimum volume: 0.0001
- command cooldown: 0.8秒
- minimum top score: 0
- minimum top1/top2 margin: 0
- time/none margin: 1.5
- time/top2 margin: 1.0
- time required consecutive: 1

`MicSettingController.applyThresholdToVoiceRunner`はfalseなので、現在は感度SliderがPause用Runnerの音量閾値を変更しない。

### time受付制御

```text
time認識
  -> GamePhaseをPauseへ変更
  -> 1秒間、ue/sita/hidari/migi/ketteiを無視
  -> 5秒間、メニュー操作を受付
  -> 受付終了
```

受付中に再び`time`が認識されると、1秒待機と5秒受付が最初から再設定される。

### メニュー操作

`VoiceMainMenuController`は4項目の縦メニューを管理する。`ue/sita`は末尾と先頭を循環し、`kettei`は選択中の`UnityEvent`を実行する。

現在の決定イベントは、メニュー終了、リタイア、マイク設定、操作説明へ接続されている。

`MoveLeft()`と`MoveRight()`は実装済みだが、Pause Sceneの`onLeftEvents`と`onRightEvents`は空である。そのため`hidari/migi`が認識・Acceptされても、現在はログを出して安全に何もしない。

## 6. Windows音声認識経路

`VoiceAi.unity`では、ONNX方式とは別に`VoiceTextRecognizer`も有効になっている。

このComponentは`DictationRecognizer`を開始し、日本語認識結果の先頭文字から`muscle/power/yaa`を判定して、同じ`VoiceCommandAction`へ`volume=1.0`で送る。

この経路はONNXの音量ゲート・スコア・none marginを通らない。ONNXと同時にWindows音声認識がマイクを利用するため、競合、二重実行、停止時警告の原因になり得る。

## 7. Time.timeScaleの影響

ゲーム用`VoiceOnnxRunner`の推論タイマーは`Time.deltaTime`を使用する。Pause中は`Time.timeScale=0`になるため、Idle中のゲーム用推論タイマーは停止する。

Pause用`VoiceOnnxRunner_Menu`と`MenuVoiceCommandAction`は`Time.unscaledDeltaTime`を使用するため、Pause中も推論、1秒待機、5秒受付が進行する。

Pause終了時は`PauseManager`が以前のtimeScaleへ戻す。

## 8. プロジェクト固有スクリプトの設計

### 8.1 Bootと常駐Manager

アプリ全体で使用するManagerは、`AppLifetimeSingleton<T>`と`AppManagerBase<T>`を基盤にしている。

`AppLifetimeSingleton<T>`の役割:

- `Boot` Sceneからだけ生成を許可する
- 重複インスタンスを破棄する
- rootへ移動して`DontDestroyOnLoad`にする
- アプリ終了前に破棄された場合、Editor/Development Buildでは例外を発生させる

`AppManagerBase<T>`の役割:

- `None / Initializing / Initialized / Failed`の初期化状態を持つ
- Coroutineによる非同期初期化を共通化する
- 二重初期化を防止する

`AppBootstrapper`はBoot時に次の順序でManagerを生成・初期化する。

```text
ContextManager
  -> RuntimeDebugManager
  -> SoundManager
  -> SceneLoader
  -> GamePhaseManager
  -> SceneProfileManager
  -> PauseManager
```

この順序により、`PauseManager`がPause Sceneを事前ロードする時点では、Sceneロード、Phase管理、Context登録の基盤が利用可能になっている。

`BootSceneRouter`はManager初期化完了後、通常は`GamePhase.InGame`へ遷移する。Editorから特定Sceneを直接再生した場合は、保存されたEditor用対象Sceneを経由してPhaseを同期する。

### 8.2 GamePhaseとSceneロード

`GamePhaseManager`は、ゲーム状態とメインSceneの対応を管理する。

通常の`Title / InGame / Result`遷移では、対応Sceneを`LoadSceneMode.Single`でロードする。Pauseだけは特殊扱いで、`PauseManager`に表示切替を依頼する。

Pause復帰時は、Pause前のPhaseに対応するSceneが現在もActive SceneならSingleロードを行わず、Phaseだけを復元する。これにより、Pauseを閉じるたびに`Prototype`とAdditive Scene一式を作り直さない。

`SceneLoader`は全Sceneロードを直列化する。Additiveロード後、そのScene内で最初に見つかった`AdditiveSceneObjectDisabler`を実行し、単体確認用CameraやLightなどを無効にする。

`VoiceAi.unity`の`AdditiveSceneObjectDisabler`は単体確認用オブジェクトを対象としており、`VoiceAIManager`は無効化対象に含まれていない。

`Prototype`では共通の`SceneCoreBase`ではなく、同等処理を持つ`ProttypeSceneCore`が使用されている。クラス名の`Prottype`は既存のスペルであり、別の概念を表すものではない。

### 8.3 ContextによるScene間参照

Additive Scene間ではUnity Inspectorの直接参照を保存しにくいため、このプロジェクトでは`ContextManager`をサービスロケータとして使用する。

`ContextBase`は実行順`-1000`の`Awake()`で自身を登録する。Context名が空の場合はGameObject名が登録名になる。

```text
VoiceAi Scene
VoiceCommandAction
  -> VoiceAiContext
       登録名/検索名: VoiceAiContext / PlayerContext

Player Scene
PlayerContext
  -> PlayerPoseContoroller（同一Sceneの直接参照）
  -> PoseContextを名前検索

PoseParryJudge Scene
PoseContext
  -> PoseParryJudge
  -> PoseStateTracker
```

音声からポーズまでの実際のクロスScene経路は次のとおり。

```text
VoiceCommandAction.ResolvePlayerPoseController()
  -> VoiceAiContext.GetPlayerContext()
  -> ContextManager.Get<PlayerContext>("PlayerContext")
  -> PlayerContext.GetPlayerPoseContoroller
  -> PlayerPoseContoroller.TryOnPose()
```

`VoiceAiContext`はPlayer Sceneより先にStartしても、`GetPlayerContext()`が呼ばれるたびに未解決参照を再検索する。`VoiceCommandAction`も`playerPoseController`がnullならコマンド実行時に再解決する。この二段階の再試行がAdditiveロード順への対策になっている。

同じContext名が複数登録された場合、`ContextManager`はエラーを出し、後から登録されたContextで上書きする。重複Sceneロード時には参照先が切り替わる可能性がある。

### 8.4 ポーズ処理とゲームルール

音声層は`PoseID`へ変換して`TryOnPose()`を呼ぶところまでを担当する。ポーズ成立後のゲームルールはPlayer/Pose側の責務である。

`PlayerPoseContoroller.TryOnPose()`のガード:

- `InGameFlowLock.IsPresentationPlaying == true`なら拒否
- すでにポーズ中なら拒否

`OnPose()`で行われる処理:

- 3秒間のポーズ状態を開始
- `PoseStateTracker.BeginPose()`を呼ぶ
- `PoseParryJudge.StartPlayerPose()`へPoseIDを渡す
- Playerの色を変更する
- Animatorへ`Yaa / Power / Muscle` Triggerを送る
- `OnPoseStart`イベントを通知する

`PoseParryJudge`は敵とPlayerの`PoseID`が同じ場合にパリイ成功イベントと紙吹雪を実行する。音声認識はパリイ成否を直接判定しない。

`InGameFlowLock`は参照カウント式の静的ロックで、ゲーム開始演出などのPresentation中にPlayer、敵、移動、観客リクエストなどを同時に止めるための仕組みである。音声推論そのものは継続できるが、最終的なポーズ実行は拒否される。

### 8.5 Pause UIの責務分担

Pause関連は次の3段階に分かれている。

```text
PauseManager
  Sceneロード、Time.timeScale、Pause状態を所有

VoiceMainMenuController
  MainMenuPanel、カーソル、選択Index、UnityEventを所有

MicSettingController / HowToController
  各サブパネル内部のカーソルと入力を所有
```

`VoiceMainMenuController.OpenMenu()/CloseMenu()`は、通常は`GamePhaseManager`または`PauseManager`へ処理を戻す。`PauseManager.IsApplyingPauseView`は、この相互呼び出しが再帰しないようにするためのフラグである。

`MicSettingController.mainMenuPanel`と`HowToController.mainMenuPanel`は、`VOICE` rootではなく`MainMenuPanel`を参照している。サブ画面を開いても`VOICE`、`MicrophoneInput`、`VoiceOnnxRunner_Menu`を停止しないための重要なScene設定である。

`MenuVoiceCommandAction`からControllerへの通知はinterfaceや直接メソッド呼び出しではなく、`SendMessage("OnVoiceCommand", command)`を使用している。対象Componentに受信関数がなくても例外にしない設計だが、コンパイル時に接続ミスを検出できない。

`MicSettingController`と`HowToController`にも`OnVoiceCommand()`は存在するが、現在の`MenuVoiceCommandAction`はメインの`VoiceMainMenuController`へ送る。サブパネルが開いている間の音声ルーティングは実装されていないため、音声コマンドが非表示のメインメニューを操作する可能性がある。

### 8.6 Scene Profileと一時デバッグ

`SceneProfileManager`はScene単位の推定フレーム時間を測る開発用Managerである。有効な測定対象Sceneのrootを一時的に無効化し、前後のフレーム時間差を記録する。

現在の`SceneProfileSettings`では`VoiceAi.isEnabled == false`なので、通常の自動測定でVoiceAiのrootを無効化しない。これは実行機能のON/OFF設定ではなく、プロファイリング対象の選択値である。

`VoiceAIManagerActiveTrace`、`SharedMicTrace`、`MicCompare`、`MenuAllScores`、`PoseTrace`などは原因調査のために追加された一時ログであり、ゲーム仕様を構成する機能ではない。

### 8.7 Assemblyと依存境界

音声、Game Core、Player、Poseの主要スクリプトには専用asmdefがなく、多くが`Assembly-CSharp`へコンパイルされる。

したがって、フォルダ上は機能分割されていても、コンパイラによるレイヤー依存制約はない。例えば音声側からPlayerやGamePhaseへ直接参照できる。外部レビューでは「現在コンパイルできること」と「望ましい依存方向であること」を分けて評価する必要がある。

Scene YAMLに残る`asdfTest.Sandbox::VoiceMenuController`などの古い`m_EditorClassIdentifier`は、現在のファイル名やクラス名と一致しない箇所がある。Unityは主に`.meta`のGUIDでMonoScriptを参照するため現状は動作しているが、Scene再保存、asmdef導入、namespace変更時にはMissing Script化しないか確認が必要である。

## 9. 責務とデータ所有の分解

| 層 | 主なクラス | 所有するもの | 所有しないもの |
|---|---|---|---|
| アプリ基盤 | `AppBootstrapper`, `SceneLoader`, `GamePhaseManager` | Manager初期化、Scene、Phase | 音声データ、認識結果 |
| 録音 | `MicrophoneInput` | デバイス選択、共有AudioClip、UseCount、PCM取得 | MFCC、モデル、コマンド |
| 特徴量 | `MfccFeatureExtractor`, `PythonCompatMfccFeatureExtractor` | PCMからMFCCへの変換 | マイク寿命、ONNX、ゲーム処理 |
| 推論 | `VoiceOnnxRunner`, `VoiceOnnxRunner_Menu` | Worker、Tensor、ラベル判定、スコアフィルタ | ポーズ/UIの具体的挙動 |
| コマンド変換 | `VoiceCommandAction`, `MenuVoiceCommandAction` | 文字列からゲーム操作へのマッピング | モデル計算、描画 |
| ゲーム処理 | `PlayerPoseContoroller`, `PoseParryJudge` | ポーズ状態、Animator、パリイ判定 | 音声スコア |
| Pause UI | `VoiceMainMenuController`ほか | カーソル、Panel、UnityEvent | マイク、MFCC、ONNX |

### オブジェクト寿命

| 対象 | 寿命 |
|---|---|
| App Manager群 | Bootからアプリ終了まで。`DontDestroyOnLoad` |
| `VoiceAIManager` | `VoiceAi` Additive Sceneの寿命 |
| Pause `VOICE` | 事前ロードされた`Pause` Sceneの寿命 |
| `SharedRecordings` | static。エントリは最後の利用者が離れるまで |
| ONNX Worker | Runnerの`Start()`から`OnDisable()`まで |
| Context | 所属Additive SceneのロードからUnloadまで |
| Playerのポーズ | 1回3秒。演出ロックや既存ポーズで開始拒否 |

### 設定値の所在

同じ設定に複数の定義場所があるため、レビュー時は優先順位に注意する。

```text
C#フィールド初期値
  < Scene / PrefabのSerializeField値
  < 実行中にSetterなどから代入された値
```

現在、ゲームRunnerとPause Runnerの実行値はSceneに保存された値が基準である。Pauseのマイク感度Sliderは`applyThresholdToVoiceRunner=false`なので、Runner値を実行中に上書きしない。

## 10. 外部レビューガイド

### 主要ファイル索引

| 分類 | パス | 確認内容 |
|---|---|---|
| 起動 | `Assets/Game/Core/Manager/AppBootstrapper.cs` | Manager生成順 |
| 常駐基盤 | `Assets/Game/Core/Manager/AppLifetimeSingleton.cs` | Boot限定生成と寿命 |
| Scene管理 | `Assets/Game/Core/Manager/SceneLoader.cs` | Single/Additive/Unload |
| Phase管理 | `Assets/Game/Core/Manager/GamePhaseManager.cs` | Pauseを含む状態遷移 |
| Phase設定 | `Assets/Game/Features/Phase/Resources/GamePhaseSceneSettings.asset` | PhaseとScene名の対応 |
| Pause管理 | `Assets/Game/Features/Pause/Script/PauseManager.cs` | 事前ロード、表示、timeScale |
| Context基盤 | `Assets/Game/Core/ContextBase.cs`, `Assets/Game/Core/Manager/ContextManager.cs` | Scene間参照 |
| 統合Scene | `Assets/_Shared/Prototype/Scenes/Prototype.unity` | Additive Scene一覧 |
| ゲーム音声Scene | `Assets/_Shared/TaiyouVoiceAI/Scenes/VoiceAi.unity` | ゲーム用Inspector参照 |
| Pause Scene | `Assets/_Shared/InGameUI/Menu/Scenes/Pause.unity` | Pause用Inspector参照とUI Event |
| 共有録音 | `Assets/_Shared/TaiyouVoiceAI/Scripts/MicrophoneInput.cs` | デバイス、Clip、UseCount |
| ゲーム推論 | `Assets/_Shared/TaiyouVoiceAI/Scripts/OnnxRunner/VoiceOnnxRunner.cs` | 4クラス推論と安全フィルタ |
| Pause推論 | `Assets/_Shared/TaiyouVoiceAI/Scripts/OnnxRunner/VoiceOnnxRunner_Menu.cs` | 7クラス推論とtimeフィルタ |
| ゲームMFCC | `Assets/_Shared/TaiyouVoiceAI/Scripts/MfccFeatureExtractor.cs` | 旧モデル向け前処理 |
| Pause MFCC | `Assets/_Shared/TaiyouVoiceAI/Scripts/PythonCompatMfccFeatureExtractor.cs` | Python互換前処理 |
| ゲームAction | `Assets/_Shared/TaiyouVoiceAI/Scripts/CommandAction/VoiceCommandAction.cs` | labelからPoseIDへの変換 |
| Pause Action | `Assets/_Shared/TaiyouVoiceAI/Scripts/CommandAction/MenuVoiceCommandAction.cs` | time窓とPhase別ルーティング |
| Player | `Assets/_Shared/Player/Scripts/PlayerPoseController.cs` | ポーズ実行 |
| Pose | `Assets/_Shared/Pose/Scripts/PoseParryJudge.cs` | Playerと敵の一致判定 |
| モデル | `Assets/_Shared/TaiyouVoiceAI/Models/` | 2つのONNXとPause labels |

### 調査時点のバージョン状態

調査時点のGit HEADは`86002f6`である。本レポートはコミット内容だけでなく、作業ツリー上の次の未コミット変更も含めて記述している。

- `Pause.unity`: `timeNoneMargin=1.5`
- `VoiceOnnxRunner_Menu.cs`: `[MenuAllScores]`ログ
- 本レポート自身

今後外部レビュー結果とコードを比較するときは、対象commitだけでなく上記の作業ツリー差分も共有する必要がある。

### 推奨する確認順

1. `VoiceAi.unity`と`Pause.unity`で、実際に有効なComponentとInspector参照を確認する
2. 2つのONNXの入出力Shapeとラベル順を確認する
3. `MicrophoneInput`のAcquire、GetLatestSamples、Release、Restartを確認する
4. ゲーム用とPause用のMFCC処理を別々に確認する
5. Runnerの音量ゲート、スコアフィルタ、Worker寿命を確認する
6. Action層からContext、Player、Menuへの接続を確認する
7. Pause、Scene Unload、Component Disable後の復帰経路を確認する
8. 最後に一時ログをOFFにしてProfiler計測する

### 変更時に維持すべき契約

- ゲーム用モデル入力は現在`[1,1,20,32]`、出力は`[1,4]`
- Pause用モデル入力は`[1,1,24,64]`、出力は`[1,7]`
- ラベル配列の順番はモデル出力indexとの外部契約である
- Pause用MFCCを変更してもゲーム用MFCCへ影響させない
- `VOICE` rootをUI Panelとして非表示にしない
- 同じマイクを使う全`MicrophoneInput`は同じ共有録音へ参加する
- `none`はゲーム操作やメニュー操作を発生させない
- Pause中に進むタイマーには`unscaledDeltaTime`を使用する
- Scene間のPlayer参照は、Additiveロード順が変わっても再解決できるようにする

### 読み違えやすい要素

- `labels4.txt`という名前だが、内容は7クラスである
- `voice_menuv4_model.onnx`も現在は7クラス出力である
- `PlayerPoseContoroller`は既存のスペルミスを含む正式なC#型名である
- `ExtractDummyMfcc()`はDummy特徴量ではなく、現在は`ExtractMfcc()`をそのまま呼ぶ互換用メソッドである
- Pause用`VOICE`に旧`MfccFeatureExtractor`も付いているが、Runnerが参照するのは`PythonCompatMfccFeatureExtractor`である
- `menuController`がScene上でnullでも、`MenuVoiceCommandAction.Start()`が同じGameObjectから取得する
- Pause Sceneがロード済みであることと、メニューUIが表示中であることは別状態である
- `SceneProfileSettings.isEnabled`はゲーム機能の有効/無効ではなく、測定対象かどうかを表す
- ONNX出力値はSoftmax確率ではなくlogitとして扱われ、marginもlogit差で判定される

### テスト観点

最低限、次をゲーム用とPause用で独立して確認する必要がある。

- 無言、各正解発声、雑音でのAccept/Reject
- 選択マイク名、Clip ID、UseCount、PCM min/max/RMS
- Pauseを開く前、Pause中、Resume後の両Runner
- `MicrophoneInput`片方だけのDisable/Enable
- 録音停止から`RestartRecordingIfNeeded()`が動いた後の両利用者
- Additive Sceneロード順を変えた場合のPlayerContext解決
- Presentation中と通常時の`TryOnPose()`
- `time`後1秒待機、5秒受付、Cooldown
- サブパネル表示中の音声コマンド送信先
- ログOFF時のCPU時間、GC Alloc、フレーム時間

## 11. 現在確認できる問題とリスク

### 優先度: 高

#### 共有録音の再作成

`RestartRecordingIfNeeded()`は録音停止を検出すると、他の利用者が残っていても`SharedRecordings.Remove(deviceName)`を実行して新しい録音を作成する。

このとき、再作成を行わなかった別インスタンスは古い`AudioClip`を保持したままになる可能性がある。新録音は同じデバイス名で開始されるため、別インスタンス側の`Microphone.IsRecording()`はtrueとなり、古いClipから新しいClipへ参加し直さない可能性がある。

#### Workerの再Enable

両Runnerは`OnDisable()`でONNX WorkerをDisposeするが、`OnEnable()`ではWorkerを再生成しない。

- ゲーム側: Dispose済みWorker参照が残る
- Pause側: `worker=null`になる

GameObjectまたはComponentをDisableしてから再Enableした場合、推論が再開しない可能性がある。

#### 二重音声認識

ゲーム側では`VoiceOnnxRunner`と`VoiceTextRecognizer`が同時に有効である。正式に使用する方式を1つに決めるか、明示的に排他制御する必要がある。

### 優先度: 中

#### ゲーム用MFCCの24x64と20x32

ゲーム用Extractorは24x64を生成する一方、Runnerは20x32だけをコピーしている。学習時の前処理と同一であるか、同じWAVをPythonとUnityへ入力して比較する必要がある。

#### 並行CPU推論

通常ゲーム中もPause用VOICEがActiveなため、ゲーム用0.4秒間隔とPause用0.7秒間隔の2つのCPU推論が並行する。`time`の常時待ち受けという目的には合うが、CPU負荷は増える。

#### 一時デバッグログ

現在は以下の大量ログが残っている。

- ゲームRunnerのIdle中毎フレームログ
- マイク取得ごとの複数ログ
- 共有Clip追跡ログ
- MFCC統計ログ
- 全ラベルスコアログ
- Active状態監視ログ

Unity Consoleへの大量出力は、FFTやONNXとは別にEditorのFPSを大きく低下させる可能性がある。

### 優先度: 低

- Pauseの`VOICE`に未使用の旧`MfccFeatureExtractor`が残っている
- ゲームRunnerに到達不能な重複Idle処理がある
- ゲームRunnerに未使用の`RunInferenceWithSamples()`がある
- 一部ログが実際のステート実行前に「終了」「RunOnnx開始」と表示される
- `PlayerPoseContoroller`というクラス名のスペルミスが既存設計として残っている
- `VoiceMainMenuController`のScene上の古いEditor class identifierが残っている
- 音声認識、MFCC、共有録音に対する自動テストが存在しない

## 12. 総合評価

現在の主要な呼び出し経路は接続されている。

- ゲーム: マイクからONNX、VoiceCommandAction、プレイヤーポーズまで接続済み
- Pause: マイクから7クラスONNX、受付制御、上下左右決定まで接続済み
- ゲーム用とPause用MFCCは分離済み
- マイク録音はデバイス単位で共有済み
- ONNX入力Shapeと出力クラス数は現在のモデルに一致

一方で、実運用前に優先して確認すべきなのは次の4点である。

1. 共有録音を再作成した場合の全利用者のClip更新
2. RunnerをDisable/Enableした場合のWorker再生成
3. ONNXとWindows DictationRecognizerの二重動作
4. 一時デバッグログの整理とFPS再計測

このレポートは静的なコード、Unity Sceneのシリアライズ値、実際のONNXメタデータを基に作成した。今回、コード、Scene、モデル、設定値の変更は行っていない。
