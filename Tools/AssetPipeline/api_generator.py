#!/usr/bin/env python3
"""
Tools/AssetPipeline/api_generator.py
One Roof - Multi-Provider Generative AI Bridge
Connects Google (Imagen 3 / Gemini) and OpenAI (DALL-E 3) APIs
with Antigravity and Codex agentic workflows.
"""

import argparse
import base64
import json
import os
import sys
import urllib.request
import urllib.error

def generate_openai(prompt: str, output_path: str, api_key: str = None, model: str = "dall-e-3"):
    api_key = api_key or os.environ.get("OPENAI_API_KEY")
    if not api_key:
        print("[OpenAI] Error: OPENAI_API_KEY environment variable not set.")
        print("[OpenAI] You can also run generation via Antigravity `generate_image` tool directly.")
        return False

    url = "https://api.openai.com/v1/images/generations"
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {api_key}"
    }
    payload = {
        "model": model,
        "prompt": prompt,
        "n": 1,
        "size": "1024x1024",
        "response_format": "b64_json",
        "quality": "standard"
    }

    try:
        req = urllib.request.Request(url, data=json.dumps(payload).encode("utf-8"), headers=headers)
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            b64_data = data["data"][0]["b64_json"]
            with open(output_path, "wb") as f:
                f.write(base64.b64decode(b64_data))
            print(f"[OpenAI] Generated image saved to: {output_path}")
            return True
    except Exception as e:
        print(f"[OpenAI] Generation request failed: {e}")
        return False

def generate_google(prompt: str, output_path: str, api_key: str = None):
    api_key = api_key or os.environ.get("GEMINI_API_KEY")
    if not api_key:
        print("[Google] Error: GEMINI_API_KEY environment variable not set.")
        print("[Google] You can also run generation via Antigravity `generate_image` tool directly.")
        return False

    url = f"https://generativelanguage.googleapis.com/v1beta/models/imagen-3.0-generate-002:predict?key={api_key}"
    headers = {"Content-Type": "application/json"}
    payload = {
        "instances": [{"prompt": prompt}],
        "parameters": {
            "sampleCount": 1,
            "aspectRatio": "1:1",
            "outputOptions": {"mimeType": "image/png"}
        }
    }

    try:
        req = urllib.request.Request(url, data=json.dumps(payload).encode("utf-8"), headers=headers)
        with urllib.request.urlopen(req) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            b64_data = data["predictions"][0]["bytesBase64Encoded"]
            with open(output_path, "wb") as f:
                f.write(base64.b64decode(b64_data))
            print(f"[Google] Generated image saved to: {output_path}")
            return True
    except Exception as e:
        print(f"[Google] Generation request failed: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description="Call OpenAI or Google generative image APIs for One Roof")
    parser.add_argument("--provider", choices=["openai", "google"], required=True)
    parser.add_argument("--prompt", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    os.makedirs(os.path.dirname(args.output) or ".", exist_ok=True)
    if args.provider == "openai":
        generate_openai(args.prompt, args.output)
    elif args.provider == "google":
        generate_google(args.prompt, args.output)

if __name__ == "__main__":
    main()
