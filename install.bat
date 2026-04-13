@echo off
setlocal enabledelayedexpansion

echo === VR AITuber Kit Windows インストーラー ===

set "ROOT=%~dp0"
set "BACKEND=%ROOT%backend"
set "VENV=%BACKEND%\.venv"

where python >nul 2>&1
if %errorlevel% equ 0 (
    set "PYTHON=python"
) else (
    where py >nul 2>&1
    if %errorlevel% equ 0 (
        set "PYTHON=py -3"
    ) else (
        echo Error: python または py が見つかりません。Python 3 をインストールし、PATH に追加してください。 >&2
        pause
        exit /b 1
    )
)

if not exist "%BACKEND%" (
    echo Error: backend フォルダーが見つかりません。リポジトリのルートで実行してください。 >&2
    pause
    exit /b 1
)

pushd "%BACKEND%"

echo - Python 仮想環境を作成/更新しています...
%PYTHON% -m venv "%VENV%"

if not exist "%VENV%\Scripts\python.exe" (
    echo Error: 仮想環境の作成に失敗しました。 >&2
    pause
    exit /b 1
)

set "VENV_PY=%VENV%\Scripts\python.exe"

echo - pip をアップグレードしています...
"%VENV_PY%" -m pip install --upgrade pip

if not exist requirements.txt (
    echo Error: backend\requirements.txt が見つかりません。 >&2
    pause
    exit /b 1
)

echo - 依存パッケージをインストールしています...
"%VENV_PY%" -m pip install -r requirements.txt

echo - audio および static フォルダーを確認しています...
if not exist audio mkdir audio
if not exist static mkdir static
if not exist static\remote.html (
    echo Warning: backend\static\remote.html が見つかりません。リモートUIを利用する場合は追加してください。 >&2
)

echo.
echo インストールが完了しました。
echo バックエンドを起動するには: build_backend.bat

echo Unity の実行やビルドには Unity Editor が必要です。
pause
popd
exit /b 0
