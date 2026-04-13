# vr-aivtuber-kit
Pythonで中身を使う、Unityで動かすタイプのAITuberを作りたくて始めました。

## 起動方法

### 1. Python/FastAPI サーバーを起動

```bash
./build_backend.sh
```

まずはPC単体で動作確認します。Unity Editorと同じPCで `http://localhost:8000/health` にアクセスできることを確認してください。

```
http://localhost:8000/health
```

### 2. Unity アプリの実行

1. Unity Editor でプロジェクトを開く
2. `AITuberController` がアタッチされている GameObject を選択
3. `serverUrl` を `http://localhost:8000` に変更
4. `WorldCommentUI` を割り当てる（必要なら `commentPrefab` を指定）
5. シーンを再生して動作確認

> スマホ接続や別端末からのアクセスは現時点では拡張設定として後回しにし、まずはPCだけで動かしてください。

### 3. Windows EXE / Android APK のビルド

まずはPC向けの Windows Standalone ビルドで動作確認することをおすすめします。Android APK は必要な場合に後から追加する拡張機能です。

Unity Editor のメニュー `Build > Build Windows Standalone` を選択すると、以下に出力されます。

- `Build/Windows/vr-aivtuber-kit.exe`

> Android APK のビルドはオプションです。`Build > Build Android APK` を選択して出力することもできますが、Android Build Support を追加しておく必要があります。

## 現在実装されている主な機能

- Python FastAPI から `POST /chat` で応答を取得
- Unity からサーバーへ JSON を送信して音声を受け取る
- 受け取った Base64 音声を WAV 形式に変換して再生
- `WorldCommentUI` にコメントを追加し、古いものを自動削除
- コメントのフェードインと浮遊アニメーション
- `AITuberController` で顔のブレンドシェイプ感情表現
- YouTube動画IDからコメントを取得する `POST /youtube-comments`
- 音声録音をUnity側で取得して `POST /voice` に送信する機能
- テキスト入力を送信して返答を得る機能
- スマホ接続や別端末連携は拡張機能として扱い、初期状態ではPC単体で動作します

## 追加の環境変数

- `YOUTUBE_API_KEY` : YouTube API を利用してコメントを取得する場合に必要です。
- `STT_URL` : 音声入力を文字起こしする STT サービスの URL を設定すると、`POST /voice` で音声内容を元に応答を生成できます。

## 追加機能の使い方

### YouTubeコメント取得
1. Unity側で `ChatFeatureController` を作成し、`youtubeVideoIdField` に動画IDを入力
2. `FetchYouTubeComments()` をボタンに割り当て
3. コメントが取得されると `WorldCommentUI` に順番に表示されます
4. `generate_reply=true` の設定により、YouTubeコメントを元にAIの返信を生成し、`WorldCommentUI` 上にも表示します

### 音声入力送信
1. Unity側で `ChatFeatureController` を作成し、`StartVoiceRecording()` と `StopVoiceRecordingAndSend()` をボタンに割り当て
2. マイクで録音
3. `StopVoiceRecordingAndSend()` で録音を終了すると、音声データが `/voice` に送信されます
4. バックエンドが文字起こしサービス (`STT_URL`) を使って内容を解析できれば、その内容を元にAI返信を生成します
5. 応答メッセージは `WorldCommentUI` に表示され、返答音声が再生されます

### 文字入力送信
1. Unity側で `ChatFeatureController` を作成し、`textInputField` に文字を入力
2. `SendTextInput()` をボタンに割り当て
3. `AITuberController` が既存の `/chat` 送信を使って返答を取得します

### スマホリモート操作（拡張機能）
1. バックエンドを起動した状態で、スマホや別端末のブラウザから `http://<PCのIPアドレス>:8000/remote` にアクセスします。
2. Webページからチャット送信や、タップ／なでる／げんこつ操作を送信できます。
3. Unity側に `RemoteControlController` を配置し、`serverUrl` を `http://localhost:8000` に設定しておきます。
4. Unityのモデルには `ModelInteractionController` を割り当てると、直接タップや右クリックでも反応できます。
5. `RemoteMonitorController` を追加すると、スマホ側のページにキャラクターの現在の感情と最新ストリームプレビューが表示されます。

### キャラクター状態とライブプレビュー
- スマホの操作ページに現在の感情、直近の反応、タップ数やなで数、げんこつ数を表示します。
- Unity でキャラクターの状態を定期送信し、リモートページにリアルタイムに反映します。
- Unity からフレームをキャプチャしてバックエンドに送信し、スマホ側にプレビュー画像を表示します。

### モデルの触れ合い反応
- 画面上のモデルをタップすると簡単な反応を返します。
- 何度も触ると少し怒った表情になります。
- 右クリックやリモート操作の「げんこつ」で、痛がって泣きそうになる反応も追加されています。
- クリックした位置が頭やお腹だと、反応が変わります。
- 反応時にはアニメーターのトリガーとブレンドシェイプを同時に使って表情を強化します。

### Play Mode テスト
- `Assets/Tests/PlayModeTests.cs` に Play Mode テストを追加しました。
- Unity の Test Runner で `Play Mode` テストを実行できます。
- テストを実行するには Unity Test Framework がプロジェクトにインストールされている必要があります。
- テスト内容:
  - `WorldCommentUI` がコメントを追加／削除できること
  - コメントの全削除機能が動作すること
  - `WavUtility` の WAV 変換が音声データを保持して再生成できること

### キャラクター設定画面
1. `CharacterSettingsController` を Unity シーンに追加
2. `settingsPanel` に設定画面のパネルを割り当てる
3. `openSettingsButton` に設定画面を開くボタンを割り当てる
4. `closeSettingsButton` に設定画面を閉じるボタンを割り当てる
5. `applySettingsButton` に設定を保存するボタンを割り当てる
6. `modelDropdown` に読み込み可能な 3D モデルプレハブを登録
7. `characterNameField`, `personalityPromptField`, `llmApiUrlField`, `llmModelField`, `ttsApiUrlField`, `ttsModelField`, `youtubeModeToggle`, `youtubeApiUrlField`, `youtubeApiKeyField` をそれぞれ割り当てる

設定を保存すると、`AITuberController` の `userId` と `context` にキャラクター名・性格プロンプトが反映されます。

### シーンモード設定画面
1. `SceneModeController` を Unity シーンに追加
2. `settingsPanel` にシーンモード設定パネルを割り当てる
3. `openSettingsButton` にシーンモード設定を開くボタンを割り当てる
4. `closeSettingsButton` にシーンモード設定を閉じるボタンを割り当てる
5. `applySettingsButton` に選択中のシーンモードを適用するボタンを割り当てる
6. `modeButtonPrefab` にモード選択ボタンのプレハブを割り当て、`modeButtonContainer` に配置
7. `modeTitleText`, `modeDescriptionText`, `statusText` を割り当てる
8. `customModeNameField`, `customModeDescriptionField`, `customSettingsField` を割り当てると、カスタムフリーモードで独自設定を入力可能
9. `modeDefinitions` に各シーンモードの `modeObjects` や `animators` を登録して、モードごとにアセット配置とアニメーションを切り替えられるようにする

シーンモードは次の7種類です:
- 対面お喋りモード
- 添い寝モード
- 膝枕モード
- 歩き回りながらしゃべるモード
- ライブ配信モード
- ソファに座って隣でお喋りモード
- カスタムフリーモード
