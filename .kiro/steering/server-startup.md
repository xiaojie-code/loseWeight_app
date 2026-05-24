# 服务启动清单

> 当用户说"启动服务"或"启动服务端"时，以下服务全部启动。

---

## 需要启动的服务

### 1. NestJS 主服务端

- 目录：`server/`
- 命令：`npm run start:dev`
- 地址：http://localhost:3000
- WebSocket：ws://localhost:3000/ws
- 说明：游戏核心后端，包含用户、对战、匹配、房间、排行、姿态等模块

### 2. MediaPipe 姿态识别服务

- 目录：`server/`
- 命令：`npm run pose:mediapipe`
- 地址：http://127.0.0.1:3100
- 说明：Python MediaPipe 姿态识别服务，供 NestJS PoseModule 调用

---

## 启动顺序

1. 先启动 NestJS 主服务端（它会尝试连接 MediaPipe 服务做健康检查，失败不影响启动）
2. 再启动 MediaPipe 姿态识别服务

## 验证方式

- NestJS：看到 `[Server] Running on port 3000` 即启动成功
- MediaPipe：看到 `[MediaPipePose] running on http://127.0.0.1:3100` 即启动成功
