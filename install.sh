#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
VENV_DIR="$BACKEND_DIR/.venv"

echo "=== VR AITuber Kit インストーラー ==="

if ! command -v python3 >/dev/null 2>&1; then
  echo "Error: python3 が見つかりません。Python 3 をインストールしてください。" >&2
  exit 1
fi

if [ ! -d "$BACKEND_DIR" ]; then
  echo "Error: backend ディレクトリが見つかりません。" >&2
  exit 1
fi

cd "$BACKEND_DIR"

echo "- Python 仮想環境を作成/更新しています..."
python3 -m venv "$VENV_DIR"

# shellcheck disable=SC1091
source "$VENV_DIR/bin/activate"

echo "- pip をアップグレードしています..."
python3 -m pip install --upgrade pip

echo "- 依存パッケージをインストールしています..."
python3 -m pip install -r requirements.txt

echo "- audio/ と static/ を確認しています..."
mkdir -p "$BACKEND_DIR/audio"
mkdir -p "$BACKEND_DIR/static"

if [ ! -f "$BACKEND_DIR/static/remote.html" ]; then
  echo "Warning: backend/static/remote.html が見つかりません。リモートUIを利用する場合は追加してください。" >&2
fi

echo ""
echo "インストールが完了しました。"
echo "バックエンドを起動するには:"
echo "  cd $BACKEND_DIR"
echo "  ./build_backend.sh"
echo ""
echo "Unity の実行やビルドには Unity Editor が必要です。README を確認してください。"
