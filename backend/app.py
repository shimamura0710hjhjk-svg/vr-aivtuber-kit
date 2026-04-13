import base64
import os
import time
import uuid
from typing import Optional

import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
AUDIO_DIR = os.path.join(BASE_DIR, "audio")
os.makedirs(AUDIO_DIR, exist_ok=True)

OLLAMA_URL = os.environ.get("OLLAMA_URL", "http://localhost:11434/v1/chat/completions")
OLLAMA_MODEL = os.environ.get("OLLAMA_MODEL", "llama2")
STYLE_VITS2_URL = os.environ.get("STYLE_VITS2_URL", "http://localhost:50021/api/tts")

app = FastAPI(title="VR AITuber Backend")


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


@app.get("/health")
def health():
    return {"status": "ok", "message": "backend running"}


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
