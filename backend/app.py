import base64
import os
import time
import uuid
from typing import List, Optional

import requests
from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import HTMLResponse, Response
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.join(BASE_DIR, "audio")
os.makedirs(AUDIO_DIR, exist_ok=True)

OLLAMA_URL = os.environ.get("OLLAMA_URL", "http://localhost:11434/v1/chat/completions")
OLLAMA_MODEL = os.environ.get("OLLAMA_MODEL", "llama2")
STYLE_VITS2_URL = os.environ.get("STYLE_VITS2_URL", "http://localhost:50021/api/tts")
STT_URL = os.environ.get("STT_URL")
YOUTUBE_API_KEY = os.environ.get("YOUTUBE_API_KEY")
YOUTUBE_COMMENT_URL = "https://www.googleapis.com/youtube/v3/commentThreads"

app = FastAPI(title="VR AITuber Backend")
app.mount("/static", StaticFiles(directory=os.path.join(BASE_DIR, "static")), name="static")


class ChatRequest(BaseModel):
    userId: Optional[str] = None
    prompt: str
    context: Optional[str] = None
    return_base64: bool = False


class ChatResponse(BaseModel):
    status: str
    text: str
    audio_path: Optional[str] = None
    audio_base64: Optional[str] = None
    model: str
    duration_ms: int


class YouTubeCommentRequest(BaseModel):
    video_id: str
    max_results: Optional[int] = 10
    generate_reply: bool = False
    return_base64: bool = False


class YouTubeCommentResponse(BaseModel):
    status: str
    video_id: str
    comments: List[str]
    reply_text: Optional[str] = None
    reply_audio_path: Optional[str] = None
    reply_audio_base64: Optional[str] = None
    duration_ms: int


class VoiceRequest(BaseModel):
    userId: Optional[str] = None
    audio_base64: str
    format: Optional[str] = "wav"
    language: Optional[str] = "ja"
    return_base64: bool = False


class VoiceResponse(BaseModel):
    status: str
    message: str
    audio_path: Optional[str] = None
    audio_base64: Optional[str] = None
    transcript: Optional[str] = None
    duration_ms: int


class RemoteCommandRequest(BaseModel):
    type: str
    prompt: Optional[str] = None
    event: Optional[str] = None
    region: Optional[str] = None


class RemoteCommand(BaseModel):
    id: int
    type: str
    prompt: Optional[str] = None
    event: Optional[str] = None
    region: Optional[str] = None
    timestamp: float


class RemoteState(BaseModel):
    emotion: Optional[str] = None
    last_interaction: Optional[str] = None
    tap_count: int = 0
    pet_head_count: int = 0
    pet_belly_count: int = 0
    punch_count: int = 0
    timestamp: float = 0.0


REMOTE_COMMANDS: List[RemoteCommand] = []
REMOTE_STATE: RemoteState = RemoteState(timestamp=time.time())
REMOTE_FRAME_PATH = os.path.join(BASE_DIR, "static", "latest_frame.jpg")
NEXT_REMOTE_COMMAND_ID = 1
MAX_REMOTE_COMMANDS = 200


@app.get("/health")
def health():
    return {"status": "ok", "message": "backend running"}


@app.get("/remote", response_class=HTMLResponse)
def remote_control_page():
    html_path = os.path.join(BASE_DIR, "static", "remote.html")
    if not os.path.exists(html_path):
        raise HTTPException(status_code=500, detail="Remote control page is missing.")
    with open(html_path, "r", encoding="utf-8") as f:
        return HTMLResponse(content=f.read())


@app.post("/remote/command", response_model=RemoteCommand)
def enqueue_remote_command(req: RemoteCommandRequest):
    global NEXT_REMOTE_COMMAND_ID

    if req.type not in {"chat", "interaction"}:
        raise HTTPException(status_code=400, detail="Unsupported remote command type.")

    command = RemoteCommand(
        id=NEXT_REMOTE_COMMAND_ID,
        type=req.type,
        prompt=req.prompt,
        event=req.event,
        region=req.region,
        timestamp=time.time(),
    )
    NEXT_REMOTE_COMMAND_ID += 1
    REMOTE_COMMANDS.append(command)
    if len(REMOTE_COMMANDS) > MAX_REMOTE_COMMANDS:
        REMOTE_COMMANDS.pop(0)
    return command


@app.get("/remote/commands", response_model=List[RemoteCommand])
def get_remote_commands(last_id: int = Query(0, ge=0)):
    return [cmd for cmd in REMOTE_COMMANDS if cmd.id > last_id]


@app.get("/remote/state", response_model=RemoteState)
def get_remote_state():
    return REMOTE_STATE


@app.post("/remote/state", response_model=RemoteState)
def update_remote_state(state: RemoteState):
    global REMOTE_STATE
    REMOTE_STATE = RemoteState(
        emotion=state.emotion,
        last_interaction=state.last_interaction,
        tap_count=state.tap_count,
        pet_head_count=state.pet_head_count,
        pet_belly_count=state.pet_belly_count,
        punch_count=state.punch_count,
        timestamp=time.time(),
    )
    return REMOTE_STATE


@app.post("/remote/frame")
def upload_remote_frame(frame: dict):
    frame_base64 = frame.get("frame_base64")
    if not frame_base64:
        raise HTTPException(status_code=400, detail="Missing frame_base64 field.")

    try:
        frame_bytes = base64.b64decode(frame_base64)
    except Exception as exc:
        raise HTTPException(status_code=400, detail=f"Invalid base64 frame data: {exc}")

    with open(REMOTE_FRAME_PATH, "wb") as f:
        f.write(frame_bytes)

    return {"status": "ok", "timestamp": time.time()}


@app.get("/remote/frame")
def get_remote_frame():
    if not os.path.exists(REMOTE_FRAME_PATH):
        raise HTTPException(status_code=404, detail="Frame not available yet.")

    with open(REMOTE_FRAME_PATH, "rb") as f:
        data = f.read()

    return Response(content=data, media_type="image/jpeg")


@app.post("/chat", response_model=ChatResponse)
def chat(req: ChatRequest):
    start_time = time.time()
    prompt = build_prompt(req.prompt, req.context)

    try:
        text = query_ollama(prompt)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Ollama request failed: {exc}")

    try:
        audio_path = synthesize_audio(text)
        audio_base64 = encode_audio_base64(audio_path) if req.return_base64 else None
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"TTS request failed: {exc}")

    duration_ms = int((time.time() - start_time) * 1000)
    return ChatResponse(
        status="ok",
        text=text,
        audio_path=audio_path,
        audio_base64=audio_base64,
        model=OLLAMA_MODEL,
        duration_ms=duration_ms,
    )


@app.post("/youtube-comments", response_model=YouTubeCommentResponse)
def youtube_comments(req: YouTubeCommentRequest):
    if not YOUTUBE_API_KEY:
        raise HTTPException(status_code=400, detail="YOUTUBE_API_KEY is not configured.")

    start_time = time.time()
    comments = fetch_youtube_comments(req.video_id, req.max_results or 10)
    reply_text = None
    reply_audio_path = None
    reply_audio_base64 = None

    if req.generate_reply:
        prompt = build_youtube_reply_prompt(req.video_id, comments)
        try:
            reply_text = query_ollama(prompt)
            if req.return_base64:
                reply_audio_path = synthesize_audio(reply_text)
                reply_audio_base64 = encode_audio_base64(reply_audio_path)
        except Exception as exc:
            raise HTTPException(status_code=500, detail=f"YouTube reply generation failed: {exc}")

    duration_ms = int((time.time() - start_time) * 1000)
    return YouTubeCommentResponse(
        status="ok",
        video_id=req.video_id,
        comments=comments,
        reply_text=reply_text,
        reply_audio_path=reply_audio_path,
        reply_audio_base64=reply_audio_base64,
        duration_ms=duration_ms,
    )


@app.post("/voice", response_model=VoiceResponse)
def voice(req: VoiceRequest):
    start_time = time.time()

    try:
        voice_path = save_voice_file(req.audio_base64, req.format)
    except Exception as exc:
        raise HTTPException(status_code=400, detail=f"Invalid audio data: {exc}")

    transcript = None
    if STT_URL:
        try:
            transcript = transcribe_audio(req.audio_base64, req.language)
        except Exception as exc:
            raise HTTPException(status_code=500, detail=f"Speech-to-text failed: {exc}")

    response_text = "音声入力を受け取りました。"
    if transcript:
        response_text = query_ollama(f"次の音声入力を日本語で理解し、丁寧に返信してください。\n\n音声内容: {transcript}")
    else:
        response_text = "音声が受信されましたが、文字起こしサービスが設定されていません。"

    try:
        reply_audio_path = synthesize_audio(response_text)
        reply_audio_base64 = encode_audio_base64(reply_audio_path) if req.return_base64 else None
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"TTS request failed: {exc}")

    duration_ms = int((time.time() - start_time) * 1000)
    return VoiceResponse(
        status="ok",
        message=response_text,
        transcript=transcript,
        audio_path=reply_audio_path,
        audio_base64=reply_audio_base64,
        duration_ms=duration_ms,
    )


def build_prompt(prompt: str, context: Optional[str]) -> str:
    if context:
        return f"{context}\n\nUser: {prompt}\nAssistant:"
    return prompt


def query_ollama(prompt: str) -> str:
    payload = {
        "model": OLLAMA_MODEL,
        "messages": [
            {"role": "user", "content": prompt}
        ],
    }
    response = requests.post(OLLAMA_URL, json=payload, timeout=60)
    response.raise_for_status()

    data = response.json()
    # Ollama chat completion format
    if isinstance(data, dict):
        choices = data.get("choices") or data.get("outputs")
        if choices and len(choices) > 0:
            first = choices[0]
            if isinstance(first, dict):
                message = first.get("message") or first
                if isinstance(message, dict):
                    content = message.get("content") or message.get("text")
                else:
                    content = first.get("content") or first.get("text")
                if content:
                    return content
    raise ValueError("Unexpected Ollama response format")


def synthesize_audio(text: str) -> str:
    output_filename = f"{uuid.uuid4().hex}.wav"
    output_path = os.path.join(AUDIO_DIR, output_filename)

    payload = {"text": text}
    response = requests.post(STYLE_VITS2_URL, json=payload, timeout=120)
    response.raise_for_status()

    content_type = response.headers.get("Content-Type", "")
    if "application/json" in content_type:
        data = response.json()
        audio_base64 = data.get("audio_base64") or data.get("wav_base64") or data.get("audio")
        if not audio_base64:
            raise ValueError("TTS JSON response missing audio_base64")
        audio_bytes = base64.b64decode(audio_base64)
        with open(output_path, "wb") as f:
            f.write(audio_bytes)
        return output_path

    # binary audio data response
    with open(output_path, "wb") as f:
        f.write(response.content)
    return output_path


def encode_audio_base64(audio_path: str) -> str:
    with open(audio_path, "rb") as f:
        return base64.b64encode(f.read()).decode("utf-8")

def save_voice_file(audio_base64: str, fmt: str = "wav") -> str:
    output_filename = f"voice_{uuid.uuid4().hex}.{fmt or 'wav'}"
    output_path = os.path.join(AUDIO_DIR, output_filename)
    audio_bytes = base64.b64decode(audio_base64)
    with open(output_path, "wb") as f:
        f.write(audio_bytes)
    return output_path


def transcribe_audio(audio_base64: str, language: str = "ja") -> str:
    if not STT_URL:
        raise ValueError("STT_URL is not configured.")

    payload = {"audio_base64": audio_base64, "language": language}
    response = requests.post(STT_URL, json=payload, timeout=120)
    response.raise_for_status()

    data = response.json()
    text = data.get("text") or data.get("transcript")
    if not text:
        raise ValueError("STT response missing text")
    return text


def build_youtube_reply_prompt(video_id: str, comments: List[str]) -> str:
    sample_comments = comments[:6]
    joined = "\n".join([f"- {c}" for c in sample_comments])
    return (
        f"以下は YouTube 動画({video_id}) のコメントです。コメントの雰囲気を汲んだ日本語の返信を1つ作成してください。"
        f"\n\nコメント一覧:\n{joined}\n\n返信:"
    )


def fetch_youtube_comments(video_id: str, max_results: int) -> List[str]:
    params = {
        "part": "snippet",
        "videoId": video_id,
        "maxResults": max_results,
        "textFormat": "plainText",
        "key": YOUTUBE_API_KEY,
    }
    response = requests.get(YOUTUBE_COMMENT_URL, params=params, timeout=30)
    response.raise_for_status()

    results = response.json()
    comments = []
    for item in results.get("items", []):
        top_comment = item.get("snippet", {}).get("topLevelComment", {}).get("snippet", {})
        text = top_comment.get("textDisplay") or top_comment.get("textOriginal")
        if text:
            comments.append(text)
    return comments
