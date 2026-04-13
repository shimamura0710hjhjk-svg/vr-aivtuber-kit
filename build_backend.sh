#!/usr/bin/env bash
set -e

# backend フォルダに移動して Python 仮想環境を作成・有効化し、FastAPI サーバーを起動します。
cd "$(dirname "$0")/backend"

if [ ! -d ".venv" ]; then
    python3 -m venv .venv
fi

# shellcheck disable=SC1091
source .venv/bin/activate
python3 -m pip install --upgrade pip
python3 -m pip install -r requirements.txt
python3 -m uvicorn app:app --host 0.0.0.0 --port 8000
