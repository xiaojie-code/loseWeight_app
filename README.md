# 减肥拳击游戏 - App 版

> 体感拳击对战健身游戏，Unity 原生 App + 端侧 MediaPipe 姿态识别

## 项目结构

```
app_project/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # GameManager, EventBus, AudioManager
│   │   ├── Combat/            # CombatManager, ActionDetector, AI
│   │   ├── PoseDetection/     # IPoseProvider, PoseFrame, Providers
│   │   │   └── Providers/     # NativeMediaPipe, Mock, MoveNet
│   │   ├── Network/           # WebSocket, 匹配, 房间, 同步
│   │   ├── UI/                # 页面, HUD, 弹窗
│   │   ├── Character/         # 角色控制, 动画, 装扮
│   │   └── Platform/          # IPlatformService, 登录, 分享, 广告
│   ├── Plugins/
│   │   └── Android/
│   │       └── MediaPipePose/ # Android 原生姿态识别插件
│   └── Resources/
├── docs/
└── server/                    # NestJS 服务端
```

## 使用方法

### 1. 创建 Unity 项目

1. 打开 Unity Hub
2. 新建项目，选择 Unity 2022.3 LTS，模板选 3D (Built-in RP)
3. 项目名称：`loseWeight_app`
4. 将本目录下的 `Assets/` 内容复制到新项目的 `Assets/` 中

### 2. 配置 Android 构建

1. File → Build Settings → 切换到 Android
2. Player Settings：
   - Company Name: YourCompany
   - Product Name: 拳击大作战
   - Package Name: com.loseweight.boxing
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Android 13 (API 33)
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64

### 3. Android 原生插件

Android 原生姿态识别插件需要：
1. 在 Android Studio 中编译为 AAR
2. 依赖：MediaPipe Tasks Vision, CameraX
3. 编译后的 AAR 放到 `Assets/Plugins/Android/`

详见 `Assets/Plugins/Android/MediaPipePose/` 中的 Java 代码。

### 4. Editor 调试

在 Unity Editor 中使用 MockPoseProvider：
- Q/E: 左右直拳
- Space: 防御
- A/D: 左右闪避

## 技术栈

- Unity 2022.3 LTS
- Android: CameraX + MediaPipe Pose Landmarker
- 服务端: NestJS + WebSocket + MySQL + Redis
- 网络: NativeWebSocket
