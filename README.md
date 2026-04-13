# vr-aivtuber-kit
Pythonで中身を使う、Unityで動かすタイプのAITuberを作りたくて始めました。

## 起動方法

### 1. Python/FastAPI サーバーを起動

```bash
./build_backend.sh
```

その後、同じWi-Fi内の別端末から以下にアクセスできることを確認します。

```
http://<PCのIPアドレス>:8000/health
```

### 2. Unity アプリの実行

1. Unity Editor でプロジェクトを開く
2. `AITuberController` がアタッチされている GameObject を選択
3. `serverUrl` を `http://<PCのIPアドレス>:8000` に変更
4. `WorldCommentUI` を割り当てる（必要なら `commentPrefab` を指定）
5. シーンを再生して動作確認

### 3. Windows EXE / Android APK のビルド

Unity Editor のメニュー `Build > Build Windows Standalone` または `Build > Build Android APK` を選択すると、以下に出力されます。

- `Build/Windows/vr-aivtuber-kit.exe`
- `Build/Android/vr-aivtuber-kit.apk`

> ビルドするには Unity Editor が必要です。Android APK を作る場合は Unity に Android Build Support を追加してください。

## 現在実装されている主な機能

- Python FastAPI から `POST /chat` で応答を取得
- Unity からサーバーへ JSON を送信して音声を受け取る
- 受け取った Base64 音声を WAV 形式に変換して再生
- `WorldCommentUI` にコメントを追加し、古いものを自動削除
- コメントのフェードインと浮遊アニメーション
- `AITuberController` で顔のブレンドシェイプ感情表現
