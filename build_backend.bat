@echo off
setlocal enabledelayedexpansion

set "ROOT=%~dp0"
set "BACKEND=%ROOT%backend"
set "PYTHON=%BACKEND%\.venv\Scripts\python.exe"

if not exist "%PYTHON%" (
    echo Error: 仮想環境が見つかりません。install.bat を先に実行してください。 >&2
    pause
    exit /b 1
)

pushd "%BACKEND%"
"%PYTHON%" -m uvicorn app:app --host 0.0.0.0 --port 8000
popd
exit /b 0
