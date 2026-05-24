import { Injectable, OnModuleInit } from '@nestjs/common';
import * as poseDetection from '@tensorflow-models/pose-detection';
import * as tf from '@tensorflow/tfjs';
import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';

const jpeg = require('jpeg-js') as {
  decode: (buffer: Buffer, options?: { useTArray?: boolean }) => { width: number; height: number; data: Uint8Array | Buffer };
};

export interface PoseLandmark {
  x: number;
  y: number;
  z: number;
  v: number;
}

type JointIndexPair = [sourceIndex: number, targetIndex: number];
type PoseProvider = 'movenet' | 'mediapipe' | 'baidu' | 'tencent';

interface DecodedRgbImage {
  width: number;
  height: number;
  data: Uint8Array;
}

interface DecodedTensorImage {
  width: number;
  height: number;
  tensor: tf.Tensor3D;
}

interface BaiduBodyPart {
  x?: number;
  y?: number;
  score?: number;
}

interface BaiduPersonInfo {
  body_parts?: Record<string, BaiduBodyPart>;
  location?: {
    left?: number;
    top?: number;
    width?: number;
    height?: number;
    score?: number;
  };
}

interface MediaPipeDetectResponse {
  success?: boolean;
  error?: string;
  provider?: string;
  costMs?: number;
  width?: number;
  height?: number;
  landmarks?: PoseLandmark[];
}

@Injectable()
export class PoseService implements OnModuleInit {
  private readonly provider: PoseProvider = this.resolveProvider();

  // MoveNet 自部署方案配置，默认使用轻量单人模型，保持 /pose/detect API 不变
  private readonly MOVENET_MODEL_TYPE_NAME = String(process.env.MOVENET_MODEL_TYPE || 'lightning').toLowerCase();
  private readonly MOVENET_MODEL_URL = process.env.MOVENET_MODEL_URL || '';
  private readonly MOVENET_MODEL_PATH = process.env.MOVENET_MODEL_PATH || this.getDefaultMoveNetModelPath();
  private readonly MOVENET_MIN_POSE_SCORE = Number(process.env.MOVENET_MIN_POSE_SCORE ?? 0.12);
  private readonly MOVENET_MIN_KEYPOINT_SCORE = Number(process.env.MOVENET_MIN_KEYPOINT_SCORE ?? 0.12);
  private moveNetDetector?: poseDetection.PoseDetector;
  private moveNetDetectorLoading?: Promise<poseDetection.PoseDetector>;
  private moveNetCallCount = 0;
  private lastMoveNetCallAt?: string;
  private lastMoveNetCostMs?: number;
  private lastMoveNetError?: string;
  private moveNetLoadCostMs?: number;

  // MediaPipe Pose runs in a separate local Python service and returns MediaPipe 33 landmarks directly.
  private readonly MEDIAPIPE_ENDPOINT = process.env.MEDIAPIPE_POSE_ENDPOINT || process.env.MEDIAPIPE_POSE_URL || 'http://127.0.0.1:3100/detect';
  private readonly MEDIAPIPE_HEALTH_ENDPOINT = process.env.MEDIAPIPE_POSE_HEALTH_URL || this.MEDIAPIPE_ENDPOINT.replace(/\/detect\/?$/, '/health');
  private readonly MEDIAPIPE_TIMEOUT_MS = Number(process.env.MEDIAPIPE_TIMEOUT_MS ?? 1800);
  private mediaPipeReady = false;
  private mediaPipeCallCount = 0;
  private lastMediaPipeCallAt?: string;
  private lastMediaPipeCostMs?: number;
  private lastMediaPipeRemoteCostMs?: number;
  private lastMediaPipeError?: string;

  // 百度云人体关键点识别配置
  private readonly BAIDU_BODY_ENDPOINT = 'https://aip.baidubce.com/rest/2.0/image-classify/v1/body_analysis';
  private readonly BAIDU_TOKEN_ENDPOINT = 'https://aip.baidubce.com/oauth/2.0/token';
  private readonly BAIDU_MIN_BOX_SCORE = Number(process.env.BAIDU_BODY_MIN_BOX_SCORE ?? 0.03);
  private readonly BAIDU_MIN_KEYPOINT_SCORE = Number(process.env.BAIDU_BODY_MIN_KEYPOINT_SCORE ?? 0.2);
  private readonly BAIDU_MIN_VALID_KEYPOINTS = Number(process.env.BAIDU_BODY_MIN_VALID_KEYPOINTS ?? 4);
  private readonly POSE_OUTPUT_MIN_CONFIDENCE = Number(process.env.POSE_OUTPUT_MIN_CONFIDENCE ?? 0.45);
  private baiduAccessToken?: string;
  private baiduTokenExpiresAt = 0;
  private baiduCallCount = 0;
  private lastBaiduCallAt?: string;
  private lastBaiduCostMs?: number;
  private lastBaiduError?: string;

  async onModuleInit() {
    if (this.provider === 'mediapipe') {
      try {
        await this.checkMediaPipeHealth();
        console.log(`[Pose] MediaPipe service ready at ${this.MEDIAPIPE_ENDPOINT}`);
      } catch (error) {
        this.lastMediaPipeError = error.message;
        console.warn(`[Pose] MediaPipe service health check failed: ${error.message}`);
      }
      return;
    }

    if (this.provider === 'movenet') {
      try {
        await this.ensureMoveNetDetector();
        console.log(`[Pose] MoveNet ${this.MOVENET_MODEL_TYPE_NAME} model warmed up`);
      } catch (error) {
        this.lastMoveNetError = error.message;
        console.warn(`[Pose] MoveNet warmup failed: ${error.message}`);
      }
      return;
    }

    if (this.provider !== 'baidu') {
      return;
    }

    try {
      await this.getBaiduAccessToken();
      console.log('[Pose] Baidu access token warmed up');
    } catch (error) {
      this.lastBaiduError = error.message;
      console.warn(`[Pose] Baidu access token warmup failed: ${error.message}`);
    }
  }

  // 腾讯云旧方案配置，仅作为保留回退
  private readonly TENCENT_SECRET_ID = process.env.TENCENT_SECRET_ID ?? '';
  private readonly TENCENT_SECRET_KEY = process.env.TENCENT_SECRET_KEY ?? '';
  private readonly TENCENT_REGION = process.env.TENCENT_REGION ?? 'ap-guangzhou';
  private readonly TENCENT_SERVICE = 'bda';
  private readonly TENCENT_HOST = 'bda.tencentcloudapi.com';
  private readonly TENCENT_ACTION = 'DetectBody';
  private readonly TENCENT_VERSION = '2020-03-24';

  getProvider(): PoseProvider {
    return this.provider;
  }

  getDiagnostics() {
    return {
      provider: this.provider,
      moveNetModelType: this.MOVENET_MODEL_TYPE_NAME,
      moveNetModelReady: Boolean(this.moveNetDetector),
      moveNetModelUrlConfigured: Boolean(this.MOVENET_MODEL_URL),
      moveNetModelPath: this.MOVENET_MODEL_PATH,
      moveNetModelPathExists: fs.existsSync(this.MOVENET_MODEL_PATH),
      moveNetMinPoseScore: this.MOVENET_MIN_POSE_SCORE,
      moveNetMinKeypointScore: this.MOVENET_MIN_KEYPOINT_SCORE,
      moveNetCallCount: this.moveNetCallCount,
      lastMoveNetCallAt: this.lastMoveNetCallAt,
      lastMoveNetCostMs: this.lastMoveNetCostMs,
      lastMoveNetError: this.lastMoveNetError,
      moveNetLoadCostMs: this.moveNetLoadCostMs,
      mediaPipeEndpoint: this.MEDIAPIPE_ENDPOINT,
      mediaPipeHealthEndpoint: this.MEDIAPIPE_HEALTH_ENDPOINT,
      mediaPipeTimeoutMs: this.MEDIAPIPE_TIMEOUT_MS,
      mediaPipeReady: this.mediaPipeReady,
      mediaPipeCallCount: this.mediaPipeCallCount,
      lastMediaPipeCallAt: this.lastMediaPipeCallAt,
      lastMediaPipeCostMs: this.lastMediaPipeCostMs,
      lastMediaPipeRemoteCostMs: this.lastMediaPipeRemoteCostMs,
      lastMediaPipeError: this.lastMediaPipeError,
      baiduCallCount: this.baiduCallCount,
      lastBaiduCallAt: this.lastBaiduCallAt,
      lastBaiduCostMs: this.lastBaiduCostMs,
      lastBaiduError: this.lastBaiduError,
      baiduTokenCached: Boolean(this.baiduAccessToken && Date.now() < this.baiduTokenExpiresAt),
    };
  }

  /**
   * 将 RGB 原始数据转为 BMP base64
   */
  async rgbToJpegBase64(rgbBase64: string, width: number, height: number): Promise<string> {
    const rgbBuffer = Buffer.from(rgbBase64, 'base64');

    // BMP 格式
    const rowSize = Math.ceil(width * 3 / 4) * 4; // BMP 行需要 4 字节对齐
    const pixelDataSize = rowSize * height;
    const fileSize = 54 + pixelDataSize;
    const bmp = Buffer.alloc(fileSize);

    // BMP Header
    bmp.write('BM', 0);
    bmp.writeUInt32LE(fileSize, 2);
    bmp.writeUInt32LE(54, 10);
    // DIB Header
    bmp.writeUInt32LE(40, 14);
    bmp.writeInt32LE(width, 18);
    bmp.writeInt32LE(-height, 22); // top-down
    bmp.writeUInt16LE(1, 26);
    bmp.writeUInt16LE(24, 28);
    bmp.writeUInt32LE(0, 30);

    // Pixel data (RGB → BGR for BMP)
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const srcIdx = (y * width + x) * 3;
        const dstIdx = 54 + y * rowSize + x * 3;
        bmp[dstIdx] = rgbBuffer[srcIdx + 2];     // B
        bmp[dstIdx + 1] = rgbBuffer[srcIdx + 1]; // G
        bmp[dstIdx + 2] = rgbBuffer[srcIdx];     // R
      }
    }

    return bmp.toString('base64');
  }

  /**
   * 将 RGBA 原始数据转为 BMP base64
   */
  async rgbaToJpegBase64(rgbaBase64: string, width: number, height: number): Promise<string> {
    const rgbaBuffer = Buffer.from(rgbaBase64, 'base64');

    // 创建 BMP 格式图片（腾讯云 API 支持 BMP）
    const fileSize = 54 + width * height * 3;
    const bmp = Buffer.alloc(fileSize);

    // BMP Header
    bmp.write('BM', 0);
    bmp.writeUInt32LE(fileSize, 2);
    bmp.writeUInt32LE(54, 10); // offset to pixel data
    // DIB Header
    bmp.writeUInt32LE(40, 14); // header size
    bmp.writeInt32LE(width, 18);
    bmp.writeInt32LE(-height, 22); // negative = top-down
    bmp.writeUInt16LE(1, 26); // planes
    bmp.writeUInt16LE(24, 28); // bits per pixel
    bmp.writeUInt32LE(0, 30); // no compression

    // Pixel data (BGR format for BMP)
    let offset = 54;
    for (let i = 0; i < width * height; i++) {
      bmp[offset++] = rgbaBuffer[i * 4 + 2]; // B
      bmp[offset++] = rgbaBuffer[i * 4 + 1]; // G
      bmp[offset++] = rgbaBuffer[i * 4];     // R
    }

    return bmp.toString('base64');
  }

  /**
   * 调用当前配置的人体关键点检测 API
   * @param base64Image - JPEG 图片的 base64 编码
   * @returns 关键点数组
   */
  async detectBody(base64Image: string, width?: number, height?: number): Promise<PoseLandmark[]> {
    if (this.provider === 'mediapipe') {
      return this.detectBodyWithMediaPipe(base64Image, width, height);
    }

    if (this.provider === 'movenet') {
      return this.detectBodyWithMoveNet(base64Image);
    }

    if (this.provider === 'tencent') {
      return this.detectBodyWithTencent(base64Image, width, height);
    }

    return this.detectBodyWithBaidu(base64Image, width, height);
  }

  private async checkMediaPipeHealth() {
    const response = await this.fetchWithTimeout(this.MEDIAPIPE_HEALTH_ENDPOINT, {
      method: 'GET',
      headers: { Accept: 'application/json' },
    }, this.MEDIAPIPE_TIMEOUT_MS);

    const data = await response.json().catch(() => ({}));
    if (!response.ok || data?.ok !== true) {
      throw new Error(`MediaPipe health failed: ${response.status} ${JSON.stringify(data)}`);
    }

    this.mediaPipeReady = true;
    this.lastMediaPipeError = undefined;
    return data;
  }

  private async detectBodyWithMediaPipe(base64Image: string, width?: number, height?: number): Promise<PoseLandmark[]> {
    const callId = ++this.mediaPipeCallCount;
    const startedAt = Date.now();
    this.lastMediaPipeCallAt = new Date().toISOString();
    this.lastMediaPipeError = undefined;

    try {
      const response = await this.fetchWithTimeout(this.MEDIAPIPE_ENDPOINT, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          image: this.stripImagePrefix(base64Image),
          width,
          height,
        }),
      }, this.MEDIAPIPE_TIMEOUT_MS);

      this.lastMediaPipeCostMs = Date.now() - startedAt;
      const data = await response.json() as MediaPipeDetectResponse;
      if (!response.ok || !data.success) {
        throw new Error(`MediaPipe pose ${response.status}: ${data.error ?? response.statusText}`);
      }

      this.mediaPipeReady = true;
      this.lastMediaPipeRemoteCostMs = Number(data.costMs ?? 0) || undefined;
      const landmarks = this.normalizeMediaPipeLandmarks(data.landmarks);

      if (callId <= 20 || callId % 20 === 0) {
        console.log(`[Pose] MediaPipe #${callId} image=${data.width ?? '-'}x${data.height ?? '-'} cost=${this.lastMediaPipeCostMs}ms remote=${this.lastMediaPipeRemoteCostMs ?? '-'}ms landmarks=${landmarks.length}`);
      }

      return landmarks;
    } catch (error) {
      this.mediaPipeReady = false;
      this.lastMediaPipeCostMs = Date.now() - startedAt;
      this.lastMediaPipeError = error.message;
      throw error;
    }
  }

  private normalizeMediaPipeLandmarks(landmarks: PoseLandmark[] | undefined): PoseLandmark[] {
    const normalized = Array.from({ length: 33 }, () => ({ x: 0, y: 0, z: 0, v: 0 }));
    if (!Array.isArray(landmarks)) {
      return normalized;
    }

    for (let index = 0; index < Math.min(landmarks.length, 33); index++) {
      const landmark = landmarks[index];
      normalized[index] = {
        x: this.clamp01(Number(landmark?.x ?? 0)),
        y: this.clamp01(Number(landmark?.y ?? 0)),
        z: Number.isFinite(Number(landmark?.z)) ? Number(landmark.z) : 0,
        v: this.clamp01(Number(landmark?.v ?? 0)),
      };
    }

    return normalized;
  }

  private async fetchWithTimeout(url: string, init: RequestInit, timeoutMs: number): Promise<Response> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), Math.max(1, timeoutMs));
    try {
      return await fetch(url, { ...init, signal: controller.signal });
    } finally {
      clearTimeout(timeout);
    }
  }

  private async detectBodyWithMoveNet(base64Image: string): Promise<PoseLandmark[]> {
    const callId = ++this.moveNetCallCount;
    const startedAt = Date.now();
    this.lastMoveNetCallAt = new Date().toISOString();
    this.lastMoveNetError = undefined;

    let image: DecodedTensorImage | undefined;
    try {
      image = this.decodeImageToTensor(base64Image);
      const detector = await this.ensureMoveNetDetector();
      const poses = await detector.estimatePoses(image.tensor, {
        maxPoses: 1,
        flipHorizontal: false,
      } as any);

      this.lastMoveNetCostMs = Date.now() - startedAt;
      if (callId <= 20 || callId % 20 === 0) {
        console.log(`[Pose] MoveNet #${callId} image=${image.width}x${image.height} cost=${this.lastMoveNetCostMs}ms poses=${poses.length}`);
      }

      const pose = this.pickBestMoveNetPose(poses);
      if (!pose?.keypoints?.length) {
        return [];
      }

      return this.moveNetKeypointsToMediaPipe(pose.keypoints, image.width, image.height);
    } catch (error) {
      this.lastMoveNetCostMs = Date.now() - startedAt;
      this.lastMoveNetError = error.message;
      throw error;
    } finally {
      image?.tensor.dispose();
    }
  }

  private async ensureMoveNetDetector(): Promise<poseDetection.PoseDetector> {
    if (this.moveNetDetector) {
      return this.moveNetDetector;
    }

    if (!this.moveNetDetectorLoading) {
      this.moveNetDetectorLoading = this.createMoveNetDetector();
    }

    this.moveNetDetector = await this.moveNetDetectorLoading;
    return this.moveNetDetector;
  }

  private async createMoveNetDetector(): Promise<poseDetection.PoseDetector> {
    const startedAt = Date.now();
    await tf.setBackend('cpu');
    await tf.ready();

    const config: poseDetection.MoveNetModelConfig = {
      modelType: this.resolveMoveNetModelType(),
      enableSmoothing: false,
    };

    if (this.MOVENET_MODEL_URL) {
      (config as any).modelUrl = this.MOVENET_MODEL_URL;
    } else if (fs.existsSync(this.MOVENET_MODEL_PATH)) {
      (config as any).modelUrl = this.createLocalGraphModelIoHandler(this.MOVENET_MODEL_PATH);
    }

    const detector = await poseDetection.createDetector(poseDetection.SupportedModels.MoveNet, config);
    this.moveNetLoadCostMs = Date.now() - startedAt;
    return detector;
  }

  private createLocalGraphModelIoHandler(modelJsonPath: string): tf.io.IOHandler {
    return {
      load: async () => {
        const modelRoot = path.dirname(modelJsonPath);
        const modelJson = JSON.parse(await fs.promises.readFile(modelJsonPath, 'utf8'));
        const weightsManifest = Array.isArray(modelJson.weightsManifest) ? modelJson.weightsManifest : [];
        const weightSpecs = weightsManifest.flatMap((group: any) => Array.isArray(group.weights) ? group.weights : []);
        const weightBuffers: Buffer[] = [];

        for (const group of weightsManifest) {
          const paths = Array.isArray(group.paths) ? group.paths : [];
          for (const relativePath of paths) {
            weightBuffers.push(await fs.promises.readFile(path.resolve(modelRoot, relativePath)));
          }
        }

        const weightData = this.bufferToArrayBuffer(Buffer.concat(weightBuffers));
        return {
          modelTopology: modelJson.modelTopology,
          weightSpecs,
          weightData,
          format: modelJson.format,
          generatedBy: modelJson.generatedBy,
          convertedBy: modelJson.convertedBy,
          signature: modelJson.signature,
          userDefinedMetadata: modelJson.userDefinedMetadata,
        } as tf.io.ModelArtifacts;
      },
    };
  }

  private bufferToArrayBuffer(buffer: Buffer): ArrayBuffer {
    return buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength) as ArrayBuffer;
  }

  private resolveMoveNetModelType() {
    if (this.isMoveNetThunderModel()) {
      return poseDetection.movenet.modelType.SINGLEPOSE_THUNDER;
    }

    return poseDetection.movenet.modelType.SINGLEPOSE_LIGHTNING;
  }

  private getDefaultMoveNetModelPath(): string {
    const modelDirectory = this.isMoveNetThunderModel() ? 'singlepose-thunder' : 'singlepose-lightning';
    return path.resolve(__dirname, '..', '..', '..', 'models', 'movenet', modelDirectory, '4', 'model.json');
  }

  private isMoveNetThunderModel(): boolean {
    return this.MOVENET_MODEL_TYPE_NAME.includes('thunder');
  }

  private pickBestMoveNetPose(poses: poseDetection.Pose[]): poseDetection.Pose | undefined {
    let bestPose: poseDetection.Pose | undefined;
    let bestScore = -1;

    for (const pose of poses) {
      const score = Number(pose.score ?? 0);
      if (score < this.MOVENET_MIN_POSE_SCORE) {
        continue;
      }

      if (score > bestScore) {
        bestScore = score;
        bestPose = pose;
      }
    }

    return bestPose;
  }

  private moveNetKeypointsToMediaPipe(
    keypoints: poseDetection.Keypoint[],
    imageWidth: number,
    imageHeight: number,
  ): PoseLandmark[] {
    const landmarks = Array.from({ length: 33 }, () => ({ x: 0, y: 0, z: 0, v: 0 }));

    keypoints.forEach((keypoint, sourceIndex) => {
      const targetIndex = this.getMoveNetMediaPipeIndex(keypoint, sourceIndex);
      if (targetIndex === undefined) {
        return;
      }

      const landmark = this.normalizeMoveNetKeypoint(keypoint, imageWidth, imageHeight);
      if (landmark.v > 0) {
        landmarks[targetIndex] = landmark;
      }
    });

    return landmarks;
  }

  private getMoveNetMediaPipeIndex(keypoint: poseDetection.Keypoint, sourceIndex: number): number | undefined {
    const normalizedName = String(keypoint.name ?? '')
      .toLowerCase()
      .replace(/[\s_\-]/g, '');

    const nameMap: Record<string, number> = {
      nose: 0,
      lefteye: 2,
      righteye: 5,
      leftear: 7,
      rightear: 8,
      leftshoulder: 11,
      rightshoulder: 12,
      leftelbow: 13,
      rightelbow: 14,
      leftwrist: 15,
      rightwrist: 16,
      lefthip: 23,
      righthip: 24,
      leftknee: 25,
      rightknee: 26,
      leftankle: 27,
      rightankle: 28,
    };

    if (Object.prototype.hasOwnProperty.call(nameMap, normalizedName)) {
      return nameMap[normalizedName];
    }

    const indexMap = this.coco17ToMediaPipe();
    const match = indexMap.find(([source]) => source === sourceIndex);
    return match?.[1];
  }

  private normalizeMoveNetKeypoint(
    keypoint: poseDetection.Keypoint,
    imageWidth: number,
    imageHeight: number,
  ): PoseLandmark {
    const rawX = Number(keypoint.x ?? 0);
    const rawY = Number(keypoint.y ?? 0);
    const rawScore = Number(keypoint.score ?? 0);
    const x = imageWidth && Math.abs(rawX) > 1 ? rawX / imageWidth : rawX;
    const y = imageHeight && Math.abs(rawY) > 1 ? rawY / imageHeight : rawY;
    const visibility = Number.isFinite(rawScore) ? this.clamp01(rawScore) : 0;

    return {
      x: this.clamp01(x),
      y: this.clamp01(y),
      z: 0,
      v: visibility >= this.MOVENET_MIN_KEYPOINT_SCORE ? Math.max(visibility, this.POSE_OUTPUT_MIN_CONFIDENCE) : visibility,
    };
  }

  private decodeImageToTensor(base64Image: string): DecodedTensorImage {
    const image = this.decodeImageToRgb(base64Image);
    const tensor = tf.tensor3d(image.data, [image.height, image.width, 3], 'int32');
    return {
      width: image.width,
      height: image.height,
      tensor,
    };
  }

  private decodeImageToRgb(base64Image: string): DecodedRgbImage {
    const buffer = Buffer.from(this.stripImagePrefix(base64Image), 'base64');
    if (buffer.length < 4) {
      throw new Error('MoveNet image decode failed: image buffer is empty');
    }

    if (buffer[0] === 0x42 && buffer[1] === 0x4d) {
      return this.decodeBmpToRgb(buffer);
    }

    if (buffer[0] === 0xff && buffer[1] === 0xd8) {
      return this.decodeJpegToRgb(buffer);
    }

    throw new Error('MoveNet image decode failed: only JPEG and BMP frames are supported');
  }

  private decodeJpegToRgb(buffer: Buffer): DecodedRgbImage {
    const decoded = jpeg.decode(buffer, { useTArray: true });
    const rgb = new Uint8Array(decoded.width * decoded.height * 3);

    for (let source = 0, target = 0; source < decoded.data.length; source += 4, target += 3) {
      rgb[target] = decoded.data[source];
      rgb[target + 1] = decoded.data[source + 1];
      rgb[target + 2] = decoded.data[source + 2];
    }

    return {
      width: decoded.width,
      height: decoded.height,
      data: rgb,
    };
  }

  private decodeBmpToRgb(buffer: Buffer): DecodedRgbImage {
    const pixelOffset = buffer.readUInt32LE(10);
    const width = buffer.readInt32LE(18);
    const dibHeight = buffer.readInt32LE(22);
    const height = Math.abs(dibHeight);
    const bitsPerPixel = buffer.readUInt16LE(28);
    const compression = buffer.readUInt32LE(30);

    if (width <= 0 || height <= 0) {
      throw new Error('MoveNet BMP decode failed: invalid dimensions');
    }
    if (compression !== 0 || (bitsPerPixel !== 24 && bitsPerPixel !== 32)) {
      throw new Error(`MoveNet BMP decode failed: unsupported BMP format bpp=${bitsPerPixel} compression=${compression}`);
    }

    const bytesPerPixel = bitsPerPixel / 8;
    const alignedRowSize = Math.floor((bitsPerPixel * width + 31) / 32) * 4;
    const packedRowSize = width * bytesPerPixel;
    const rowSize = buffer.length >= pixelOffset + alignedRowSize * height ? alignedRowSize : packedRowSize;

    if (buffer.length < pixelOffset + rowSize * height) {
      throw new Error('MoveNet BMP decode failed: truncated pixel data');
    }

    const topDown = dibHeight < 0;
    const rgb = new Uint8Array(width * height * 3);

    for (let y = 0; y < height; y++) {
      const sourceY = topDown ? y : height - 1 - y;
      const rowOffset = pixelOffset + sourceY * rowSize;
      for (let x = 0; x < width; x++) {
        const source = rowOffset + x * bytesPerPixel;
        const target = (y * width + x) * 3;
        rgb[target] = buffer[source + 2];
        rgb[target + 1] = buffer[source + 1];
        rgb[target + 2] = buffer[source];
      }
    }

    return { width, height, data: rgb };
  }

  private async detectBodyWithBaidu(base64Image: string, width?: number, height?: number): Promise<PoseLandmark[]> {
    const accessToken = await this.getBaiduAccessToken();
    const image = this.stripImagePrefix(base64Image);
    const requestBody = new URLSearchParams({ image });
    const endpoint = process.env.BAIDU_BODY_ENDPOINT || this.BAIDU_BODY_ENDPOINT;
    const callId = ++this.baiduCallCount;
    const startedAt = Date.now();
    this.lastBaiduCallAt = new Date().toISOString();
    this.lastBaiduError = undefined;

    if (callId <= 20 || callId % 20 === 0) {
      console.log(`[Pose] Baidu body_analysis #${callId} request bytes~${Math.round(image.length * 0.75)} endpoint=${endpoint}`);
    }

    try {
      const response = await fetch(`${endpoint}?access_token=${encodeURIComponent(accessToken)}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: requestBody.toString(),
      });

      this.lastBaiduCostMs = Date.now() - startedAt;
      const data = await response.json();
      if (!response.ok || data.error_code) {
        throw new Error(`Baidu body_analysis ${data.error_code ?? response.status}: ${data.error_msg ?? response.statusText}`);
      }

      const person = this.pickBestBaiduPerson(data.person_info);
      if (!person?.body_parts) {
        return [];
      }

      return this.baiduBodyPartsToMediaPipe(person.body_parts, width, height, person.location);
    } catch (error) {
      this.lastBaiduCostMs = Date.now() - startedAt;
      this.lastBaiduError = error.message;
      throw error;
    }
  }

  private async getBaiduAccessToken(): Promise<string> {
    const configuredToken = process.env.BAIDU_BODY_ACCESS_TOKEN || process.env.BAIDU_ACCESS_TOKEN;
    if (configuredToken) {
      return configuredToken;
    }

    if (this.baiduAccessToken && Date.now() < this.baiduTokenExpiresAt) {
      return this.baiduAccessToken;
    }

    const apiKey = process.env.BAIDU_BODY_API_KEY || process.env.BAIDU_API_KEY;
    const secretKey = process.env.BAIDU_BODY_SECRET_KEY || process.env.BAIDU_SECRET_KEY;
    if (!apiKey || !secretKey) {
      throw new Error('Baidu credentials missing: set BAIDU_BODY_API_KEY and BAIDU_BODY_SECRET_KEY');
    }

    const tokenUrl = new URL(process.env.BAIDU_TOKEN_ENDPOINT || this.BAIDU_TOKEN_ENDPOINT);
    tokenUrl.searchParams.set('grant_type', 'client_credentials');
    tokenUrl.searchParams.set('client_id', apiKey);
    tokenUrl.searchParams.set('client_secret', secretKey);

    const response = await fetch(tokenUrl, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
      },
    });

    const data = await response.json();
    if (!response.ok || data.error || !data.access_token) {
      throw new Error(`Baidu auth failed: ${data.error_description ?? data.error ?? response.statusText}`);
    }

    const accessToken = String(data.access_token);
    this.baiduAccessToken = accessToken;
    const expiresInSeconds = Number(data.expires_in || 2592000);
    this.baiduTokenExpiresAt = Date.now() + Math.max(60, expiresInSeconds - 300) * 1000;
    return accessToken;
  }

  private pickBestBaiduPerson(personInfo: unknown): BaiduPersonInfo | undefined {
    if (!Array.isArray(personInfo) || personInfo.length === 0) {
      return undefined;
    }

    let bestPerson: BaiduPersonInfo | undefined;
    let bestScore = -1;

    for (const person of personInfo as BaiduPersonInfo[]) {
      const boxScore = Number(person.location?.score ?? 0);
      const bodyParts = person.body_parts ?? {};
      const validKeypoints = Object.values(bodyParts).filter((part) => {
        const x = Number(part?.x);
        const y = Number(part?.y);
        const score = Number(part?.score ?? 0);
        return Number.isFinite(x) && Number.isFinite(y) && score >= this.BAIDU_MIN_KEYPOINT_SCORE;
      }).length;

      if (boxScore < this.BAIDU_MIN_BOX_SCORE || validKeypoints < this.BAIDU_MIN_VALID_KEYPOINTS) {
        continue;
      }

      const combinedScore = boxScore + validKeypoints / 100;
      if (combinedScore > bestScore) {
        bestScore = combinedScore;
        bestPerson = person;
      }
    }

    return bestPerson;
  }

  private baiduBodyPartsToMediaPipe(
    bodyParts: Record<string, BaiduBodyPart>,
    width?: number,
    height?: number,
    location?: BaiduPersonInfo['location'],
  ): PoseLandmark[] {
    const landmarks = Array.from({ length: 33 }, () => ({ x: 0, y: 0, z: 0, v: 0 }));
    const map: Record<string, number> = {
      nose: 0,
      left_eye: 2,
      right_eye: 5,
      left_ear: 7,
      right_ear: 8,
      left_mouth_corner: 9,
      right_mouth_corner: 10,
      left_shoulder: 11,
      right_shoulder: 12,
      left_elbow: 13,
      right_elbow: 14,
      left_wrist: 15,
      right_wrist: 16,
      left_hip: 23,
      right_hip: 24,
      left_knee: 25,
      right_knee: 26,
      left_ankle: 27,
      right_ankle: 28,
    };

    for (const [sourceName, targetIndex] of Object.entries(map)) {
      const part = bodyParts[sourceName];
      if (part) {
        landmarks[targetIndex] = this.normalizeBaiduPart(part, width, height);
      }
    }

    this.fillSyntheticHips(landmarks, location, width, height);

    return landmarks;
  }

  private normalizeBaiduPart(part: BaiduBodyPart, width?: number, height?: number): PoseLandmark {
    const rawX = Number(part.x ?? 0);
    const rawY = Number(part.y ?? 0);
    const rawScore = Number(part.score ?? 0);

    const x = width && Math.abs(rawX) > 1 ? rawX / width : rawX;
    const y = height && Math.abs(rawY) > 1 ? rawY / height : rawY;

    const visibility = Number.isFinite(rawScore) ? this.clamp01(rawScore) : 0;

    return {
      x: this.clamp01(x),
      y: this.clamp01(y),
      z: 0,
      v: visibility >= this.BAIDU_MIN_KEYPOINT_SCORE ? Math.max(visibility, this.POSE_OUTPUT_MIN_CONFIDENCE) : visibility,
    };
  }

  private fillSyntheticHips(
    landmarks: PoseLandmark[],
    location?: BaiduPersonInfo['location'],
    width?: number,
    height?: number,
  ) {
    const leftShoulder = landmarks[11];
    const rightShoulder = landmarks[12];
    if (!this.isOutputLandmarkValid(leftShoulder) || !this.isOutputLandmarkValid(rightShoulder)) {
      return;
    }

    const boxTop = Number(location?.top);
    const boxHeight = Number(location?.height);
    const hasUsableBox = width && height && Number.isFinite(boxTop) && Number.isFinite(boxHeight) && boxHeight > 0;
    const estimatedHipY = hasUsableBox
      ? this.clamp01((boxTop + boxHeight * 0.62) / height)
      : this.clamp01(((leftShoulder.y + rightShoulder.y) / 2) + 0.25);

    const confidence = Math.min(leftShoulder.v, rightShoulder.v, this.POSE_OUTPUT_MIN_CONFIDENCE);
    if (!this.isOutputLandmarkValid(landmarks[23])) {
      landmarks[23] = { x: leftShoulder.x, y: estimatedHipY, z: 0, v: confidence };
    }
    if (!this.isOutputLandmarkValid(landmarks[24])) {
      landmarks[24] = { x: rightShoulder.x, y: estimatedHipY, z: 0, v: confidence };
    }
  }

  private isOutputLandmarkValid(landmark: PoseLandmark | undefined): boolean {
    return Number(landmark?.v ?? 0) >= this.BAIDU_MIN_KEYPOINT_SCORE;
  }

  private async detectBodyWithTencent(base64Image: string, width?: number, height?: number): Promise<PoseLandmark[]> {
    if (!this.TENCENT_SECRET_ID || !this.TENCENT_SECRET_KEY) {
      throw new Error('Tencent credentials missing: set TENCENT_SECRET_ID and TENCENT_SECRET_KEY');
    }

    const timestamp = Math.floor(Date.now() / 1000);
    const date = new Date(timestamp * 1000).toISOString().split('T')[0];

    const payload = JSON.stringify({
      Image: base64Image,
      MaxBodyNum: 1,
    });

    // TC3-HMAC-SHA256 签名
    const authorization = this.sign(timestamp, date, payload);

    // 发起请求
    const response = await fetch(`https://${this.TENCENT_HOST}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
        'Host': this.TENCENT_HOST,
        'X-TC-Action': this.TENCENT_ACTION,
        'X-TC-Version': this.TENCENT_VERSION,
        'X-TC-Region': this.TENCENT_REGION,
        'X-TC-Timestamp': String(timestamp),
        'Authorization': authorization,
      },
      body: payload,
    });

    const data = await response.json();

    if (data.Response?.Error) {
      throw new Error(`${data.Response.Error.Code}: ${data.Response.Error.Message}`);
    }

    const results = data.Response?.BodyDetectResults;
    if (!results || results.length === 0) {
      return []; // 没检测到人体
    }

    // 提取关键点并转换为 Unity 侧 PoseFrame 使用的 MediaPipe 33 点索引
    const body = results[0].Body;
    if (!body?.BodyJoints) {
      return [];
    }

    return this.toMediaPipeLandmarks(body.BodyJoints, width, height);
  }

  private toMediaPipeLandmarks(joints: any[], width?: number, height?: number): PoseLandmark[] {
    const landmarks = Array.from({ length: 33 }, () => ({ x: 0, y: 0, z: 0, v: 0 }));
    if (!Array.isArray(joints) || joints.length === 0) {
      return landmarks;
    }

    const namedTargetCount = this.applyNamedJoints(joints, landmarks, width, height);
    if (namedTargetCount > 0) {
      return landmarks;
    }

    if (joints.length >= 33) {
      for (let i = 0; i < 33; i++) {
        landmarks[i] = this.normalizeJoint(joints[i], width, height);
      }
      return landmarks;
    }

    const indexMap = joints.length >= 18 ? this.openPose18ToMediaPipe() : this.coco17ToMediaPipe();
    for (const [sourceIndex, targetIndex] of indexMap) {
      if (sourceIndex < joints.length) {
        landmarks[targetIndex] = this.normalizeJoint(joints[sourceIndex], width, height);
      }
    }

    return landmarks;
  }

  private applyNamedJoints(joints: any[], landmarks: PoseLandmark[], width?: number, height?: number): number {
    let appliedCount = 0;
    for (const joint of joints) {
      const targetIndex = this.getMediaPipeIndexByName(joint);
      if (targetIndex === undefined) continue;

      landmarks[targetIndex] = this.normalizeJoint(joint, width, height);
      appliedCount++;
    }
    return appliedCount;
  }

  private normalizeJoint(joint: any, width?: number, height?: number): PoseLandmark {
    const rawX = Number(joint?.x ?? joint?.X ?? joint?.Position?.X ?? joint?.Point?.X ?? 0);
    const rawY = Number(joint?.y ?? joint?.Y ?? joint?.Position?.Y ?? joint?.Point?.Y ?? 0);
    const rawZ = Number(joint?.z ?? joint?.Z ?? 0);
    const rawConfidence = Number(
      joint?.v ?? joint?.visibility ?? joint?.Visibility ?? joint?.confidence ?? joint?.Confidence ?? joint?.score ?? joint?.Score ?? 0.9,
    );

    const x = width && Math.abs(rawX) > 1 ? rawX / width : rawX;
    const y = height && Math.abs(rawY) > 1 ? rawY / height : rawY;

    return {
      x: this.clamp01(x),
      y: this.clamp01(y),
      z: Number.isFinite(rawZ) ? rawZ : 0,
      v: Number.isFinite(rawConfidence) ? this.clamp01(rawConfidence) : 0.9,
    };
  }

  private getMediaPipeIndexByName(joint: any): number | undefined {
    const name = String(
      joint?.name ?? joint?.Name ?? joint?.type ?? joint?.Type ?? joint?.part ?? joint?.Part ?? joint?.bodyPart ?? joint?.BodyPart ?? '',
    )
      .toLowerCase()
      .replace(/[\s_\-]/g, '');

    const map: Record<string, number> = {
      nose: 0,
      鼻: 0,
      leftshoulder: 11,
      左肩: 11,
      rightshoulder: 12,
      右肩: 12,
      leftelbow: 13,
      左肘: 13,
      rightelbow: 14,
      右肘: 14,
      leftwrist: 15,
      左腕: 15,
      左手腕: 15,
      righthand: 16,
      rightwrist: 16,
      右腕: 16,
      右手腕: 16,
      lefthip: 23,
      左髋: 23,
      左胯: 23,
      righthip: 24,
      右髋: 24,
      右胯: 24,
    };

    return map[name];
  }

  private coco17ToMediaPipe(): JointIndexPair[] {
    return [
      [0, 0],
      [5, 11],
      [6, 12],
      [7, 13],
      [8, 14],
      [9, 15],
      [10, 16],
      [11, 23],
      [12, 24],
    ];
  }

  private openPose18ToMediaPipe(): JointIndexPair[] {
    return [
      [0, 0],
      [5, 11],
      [2, 12],
      [6, 13],
      [3, 14],
      [7, 15],
      [4, 16],
      [11, 23],
      [8, 24],
    ];
  }

  private clamp01(value: number): number {
    if (!Number.isFinite(value)) return 0;
    return Math.max(0, Math.min(1, value));
  }

  private stripImagePrefix(base64Image: string): string {
    return base64Image.replace(/^data:image\/[a-zA-Z0-9.+-]+;base64,/, '');
  }

  private resolveProvider(): PoseProvider {
    const provider = String(process.env.POSE_PROVIDER || 'movenet').toLowerCase();
    if (provider === 'baidu' || provider === 'tencent' || provider === 'movenet' || provider === 'mediapipe') {
      return provider;
    }

    return 'movenet';
  }

  /**
   * TC3-HMAC-SHA256 签名
   */
  private sign(timestamp: number, date: string, payload: string): string {
    const algorithm = 'TC3-HMAC-SHA256';

    // Step 1: CanonicalRequest
    const httpRequestMethod = 'POST';
    const canonicalUri = '/';
    const canonicalQueryString = '';
    const contentType = 'application/json; charset=utf-8';
    const canonicalHeaders = `content-type:${contentType}\nhost:${this.TENCENT_HOST}\nx-tc-action:${this.TENCENT_ACTION.toLowerCase()}\n`;
    const signedHeaders = 'content-type;host;x-tc-action';
    const hashedPayload = this.sha256(payload);
    const canonicalRequest = `${httpRequestMethod}\n${canonicalUri}\n${canonicalQueryString}\n${canonicalHeaders}\n${signedHeaders}\n${hashedPayload}`;

    // Step 2: StringToSign
    const credentialScope = `${date}/${this.TENCENT_SERVICE}/tc3_request`;
    const hashedCanonicalRequest = this.sha256(canonicalRequest);
    const stringToSign = `${algorithm}\n${timestamp}\n${credentialScope}\n${hashedCanonicalRequest}`;

    // Step 3: Signature
    const secretDate = this.hmacSha256(`TC3${this.TENCENT_SECRET_KEY}`, date);
    const secretService = this.hmacSha256(secretDate, this.TENCENT_SERVICE);
    const secretSigning = this.hmacSha256(secretService, 'tc3_request');
    const signature = crypto
      .createHmac('sha256', secretSigning)
      .update(stringToSign)
      .digest('hex');

    // Step 4: Authorization
    return `${algorithm} Credential=${this.TENCENT_SECRET_ID}/${credentialScope}, SignedHeaders=${signedHeaders}, Signature=${signature}`;
  }

  private sha256(message: string): string {
    return crypto.createHash('sha256').update(message).digest('hex');
  }

  private hmacSha256(key: string | Buffer, message: string): Buffer {
    return crypto.createHmac('sha256', key).update(message).digest();
  }
}
