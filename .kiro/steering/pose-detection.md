---
inclusion: auto
---

# 体感拳击项目 — 核心技术要求

## 绝对原则

1. **必须使用真正的 AI 姿态识别**（如 Google MediaPipe、TensorFlow MoveNet、PoseNet 等），禁止自己编写帧差法、肤色检测等替代方案
2. **高灵敏、高识别度、及时反馈** — 这是核心需求，不可妥协
3. AI 姿态识别能精确定位人体关键点（手腕、肘部、肩膀等），基于关键点的位置和速度判断出拳动作

## 技术限制备忘

### 微信小游戏环境限制
- 微信小游戏不是标准浏览器，没有 `navigator.mediaDevices`、`<video>`、`<canvas>` DOM 等 Web API
- MediaPipe Pose 依赖标准 Web API + WASM SIMD + 动态模块加载，在微信小游戏中完全不可用
- 微信小程序主包限制 2MB，TF.js npm 包超限
- 微信 TF.js 插件（wx6afed118d9e81df9）需要授权，流程复杂
- `wx.createInferenceSession` 对 ONNX 模型格式兼容性差
- HuggingFace / hf-mirror.com 在国内手机端无法访问
- 帧差法、肤色检测等方案精度不够，已验证不可行

### VKSession body tracking 限制（2025.5 实测）
- 微信 VKSession body tracking 在 Android 设备上覆盖率极低
- 小米14 Pro（骁龙8 Gen 3）+ 微信 8.0.71 测试失败，errno: 2003002（设备不支持）
- `version: 'v1'`、`version: 'v2'`、不传 version 三种方式均失败
- 该能力可能仅在部分 iPhone（A12+）和少数 Android 旗舰上可用
- 不适合作为主要方案，设备覆盖率无法保证

### 推荐替代方案：wx.createCamera + 腾讯云人体分析 API
- `wx.createCamera` 所有设备都支持，可获取摄像头帧数据
- 腾讯云人体关键点分析 API 返回 14/17 个关键点，足够做出拳/防御/闪避检测
- 延迟 100-200ms（WiFi/5G），10-15 FPS 调用频率够用
- 成本：约 0.36 元/局（60秒 × 15fps = 900次调用，预付费包更低）
- 正式环境需通过自有服务器中转（保护 SecretKey，防盗刷）
- 架构：wx.createCamera → 取帧压缩 → 服务器中转 → 腾讯云 API → WebSocket 回传 → Unity

### 正式环境：服务端中转（必须）
- **直连方案不能用于正式环境**，原因：
  1. SecretId/SecretKey 会暴露在前端代码中，用户反编译即可盗用
  2. 无法做调用频率限制和防盗刷
- **正式环境架构：**
  ```
  小游戏 (wx.createCamera 取帧)
      ↓ wx.request 上传 JPEG 帧（base64）
  自有服务器 (NestJS，server/ 目录)
      ├─ 验证请求来源（token/签名校验）
      ├─ 频率限制（防恶意刷接口）
      ├─ 用 SecretKey 签名调用腾讯云 API
      └─ 返回关键点数据
      ↓ WebSocket / HTTP Response
  小游戏 → SendMessage → Unity ActionDetector
  ```
- **服务端需要做的事：**
  1. 一个 POST 接口接收帧图片（base64 JPEG，约 10-30KB/帧）
  2. 用腾讯云 SDK 调用人体关键点分析 API
  3. 将关键点结果返回给客户端
  4. 加 token 验证 + 频率限制（每用户每秒最多 15 次）
- **开发阶段可以先用直连快速验证效果，上线前必须切到服务端中转**


