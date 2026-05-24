# MediaPipe Pose Service

This local service runs Google MediaPipe Pose outside the WeChat mini-game runtime and returns MediaPipe 33 landmarks for the existing NestJS `/pose/detect` endpoint.

## Setup

```powershell
cd server
python -m pip install -r pose-mediapipe/requirements.txt
```

## Run

```powershell
cd server
python pose-mediapipe/mediapipe_pose_server.py
```

Default endpoint: `http://127.0.0.1:3100/detect`

Environment variables:

- `MEDIAPIPE_HOST`, default `127.0.0.1`
- `MEDIAPIPE_PORT`, default `3100`
- `MEDIAPIPE_MODEL_COMPLEXITY`, default `0`
- `MEDIAPIPE_MIN_DETECTION_CONFIDENCE`, default `0.35`
- `MEDIAPIPE_MIN_TRACKING_CONFIDENCE`, default `0.35`