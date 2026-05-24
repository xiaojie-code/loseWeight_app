import base64
import json
import os
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import cv2
import mediapipe as mp
import numpy as np


HOST = os.environ.get("MEDIAPIPE_HOST", "127.0.0.1")
PORT = int(os.environ.get("MEDIAPIPE_PORT", "3100"))
MODEL_COMPLEXITY = int(os.environ.get("MEDIAPIPE_MODEL_COMPLEXITY", "0"))
MIN_DETECTION_CONFIDENCE = float(os.environ.get("MEDIAPIPE_MIN_DETECTION_CONFIDENCE", "0.35"))
MIN_TRACKING_CONFIDENCE = float(os.environ.get("MEDIAPIPE_MIN_TRACKING_CONFIDENCE", "0.35"))
OUTPUT_MIN_CONFIDENCE = float(os.environ.get("MEDIAPIPE_OUTPUT_MIN_CONFIDENCE", "0.45"))


mp_pose = mp.solutions.pose
pose_lock = threading.Lock()
pose = mp_pose.Pose(
    static_image_mode=False,
    model_complexity=MODEL_COMPLEXITY,
    smooth_landmarks=True,
    enable_segmentation=False,
    min_detection_confidence=MIN_DETECTION_CONFIDENCE,
    min_tracking_confidence=MIN_TRACKING_CONFIDENCE,
)

stats = {
    "detectCallCount": 0,
    "lastDetectAt": None,
    "lastCostMs": None,
    "lastError": None,
}


def strip_data_url(value: str) -> str:
    comma_index = value.find(",")
    return value[comma_index + 1 :] if comma_index >= 0 else value


def decode_image(payload: dict) -> np.ndarray:
    if payload.get("image"):
        image_bytes = base64.b64decode(strip_data_url(str(payload["image"])))
        encoded = np.frombuffer(image_bytes, dtype=np.uint8)
        image_bgr = cv2.imdecode(encoded, cv2.IMREAD_COLOR)
        if image_bgr is None:
            raise ValueError("image decode failed")
        return cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)

    width = int(payload.get("width") or 0)
    height = int(payload.get("height") or 0)
    if width <= 0 or height <= 0:
        raise ValueError("width and height are required for raw frames")

    if payload.get("imageRgb"):
        raw = base64.b64decode(strip_data_url(str(payload["imageRgb"])))
        rgb = np.frombuffer(raw, dtype=np.uint8).reshape((height, width, 3))
        return rgb.copy()

    if payload.get("imageRgba"):
        raw = base64.b64decode(strip_data_url(str(payload["imageRgba"])))
        rgba = np.frombuffer(raw, dtype=np.uint8).reshape((height, width, 4))
        return cv2.cvtColor(rgba, cv2.COLOR_RGBA2RGB)

    raise ValueError("image data is required")


def landmarks_to_json(results) -> list[dict]:
    landmarks = [{"x": 0, "y": 0, "z": 0, "v": 0} for _ in range(33)]
    if not results.pose_landmarks:
        return []

    for index, landmark in enumerate(results.pose_landmarks.landmark[:33]):
        visibility = float(getattr(landmark, "visibility", 0.0) or 0.0)
        output_visibility = visibility
        if visibility > 0:
            output_visibility = max(min(visibility, 1.0), OUTPUT_MIN_CONFIDENCE)
        landmarks[index] = {
            "x": min(max(float(landmark.x), 0.0), 1.0),
            "y": min(max(float(landmark.y), 0.0), 1.0),
            "z": float(landmark.z),
            "v": min(max(output_visibility, 0.0), 1.0),
        }

    return landmarks


def detect_pose(payload: dict) -> dict:
    started_at = time.time()
    stats["detectCallCount"] += 1
    stats["lastDetectAt"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    stats["lastError"] = None

    try:
        image_rgb = decode_image(payload)
        image_rgb.flags.writeable = False
        with pose_lock:
            results = pose.process(image_rgb)
        landmarks = landmarks_to_json(results)
        cost_ms = int((time.time() - started_at) * 1000)
        stats["lastCostMs"] = cost_ms
        return {
            "success": True,
            "provider": "mediapipe",
            "costMs": cost_ms,
            "width": int(image_rgb.shape[1]),
            "height": int(image_rgb.shape[0]),
            "landmarks": landmarks,
        }
    except Exception as exc:
        cost_ms = int((time.time() - started_at) * 1000)
        stats["lastCostMs"] = cost_ms
        stats["lastError"] = str(exc)
        raise


class Handler(BaseHTTPRequestHandler):
    server_version = "MediaPipePoseHTTP/1.0"

    def do_GET(self):
        if self.path.split("?", 1)[0] != "/health":
            self.send_json(404, {"ok": False, "error": "not found"})
            return

        self.send_json(
            200,
            {
                "ok": True,
                "provider": "mediapipe",
                "modelComplexity": MODEL_COMPLEXITY,
                "minDetectionConfidence": MIN_DETECTION_CONFIDENCE,
                "minTrackingConfidence": MIN_TRACKING_CONFIDENCE,
                **stats,
            },
        )

    def do_POST(self):
        if self.path.split("?", 1)[0] != "/detect":
            self.send_json(404, {"success": False, "error": "not found"})
            return

        try:
            length = int(self.headers.get("Content-Length") or 0)
            payload = json.loads(self.rfile.read(length).decode("utf-8")) if length else {}
            self.send_json(200, detect_pose(payload))
        except Exception as exc:
            self.send_json(200, {"success": False, "error": str(exc), "provider": "mediapipe"})

    def log_message(self, format, *args):
        return

    def send_json(self, status_code: int, payload: dict):
        data = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


def main():
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print(
        f"[MediaPipePose] running on http://{HOST}:{PORT} "
        f"model_complexity={MODEL_COMPLEXITY} min_detection={MIN_DETECTION_CONFIDENCE}"
    )
    server.serve_forever()


if __name__ == "__main__":
    main()