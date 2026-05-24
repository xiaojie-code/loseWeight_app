import { Controller, Post, Body, Get } from '@nestjs/common';
import { PoseLandmark, PoseService } from './pose.service';

@Controller('pose')
export class PoseController {
  private detectRequestCount = 0;
  private lastDetectAt?: string;

  constructor(private readonly poseService: PoseService) {}

  @Get('status')
  getStatus() {
    return {
      ok: true,
      detectRequestCount: this.detectRequestCount,
      lastDetectAt: this.lastDetectAt,
      diagnostics: this.poseService.getDiagnostics(),
    };
  }

  /**
    * 接收摄像头帧数据，调用当前配置的人体关键点 API
   * 支持两种格式：
   * 1. { image: "base64 JPEG" } - 直接 JPEG 图片
   * 2. { imageRgba: "base64 RGBA", width: number, height: number } - 原始 RGBA 数据
   */
  @Post('detect')
  async detectBody(@Body() body: { image?: string; imageRgba?: string; imageRgb?: string; width?: number; height?: number }) {
    this.detectRequestCount++;
    this.lastDetectAt = new Date().toISOString();
    const requestId = this.detectRequestCount;

    try {
      let base64Image: string;
      let inputType = 'unknown';

      if (body.image) {
        base64Image = body.image;
        inputType = 'image';
      } else if (body.imageRgb && body.width && body.height) {
        base64Image = await this.poseService.rgbToJpegBase64(body.imageRgb, body.width, body.height);
        inputType = 'imageRgb';
      } else if (body.imageRgba && body.width && body.height) {
        base64Image = await this.poseService.rgbaToJpegBase64(body.imageRgba, body.width, body.height);
        inputType = 'imageRgba';
      } else {
        console.warn(`[Pose] #${requestId} rejected: image data is required`);
        return { success: false, error: 'image data is required' };
      }

      const startedAt = Date.now();
      const imageBytes = Math.round(this.stripImagePrefix(base64Image).length * 0.75);
      console.log(`[Pose] #${requestId} detect input=${inputType} size=${body.width ?? '-'}x${body.height ?? '-'} bytes~${imageBytes}`);
      const landmarks = await this.poseService.detectBody(base64Image, body.width, body.height);
      const debug = this.buildLandmarkDebug(landmarks);
      console.log(`[Pose] #${requestId} done cost=${Date.now() - startedAt}ms provider=${this.poseService.getProvider()} valid=${debug.validRequiredCount}/${debug.requiredCount} missing=${debug.missing.join(',') || 'none'}`);
      return {
        success: true,
        provider: this.poseService.getProvider(),
        debug,
        landmarks,
      };
    } catch (error) {
      const message = error.message ?? String(error);
      const rateLimited = /qps|limit|rate|too many/i.test(message);
      console.error(`[Pose] #${requestId} failed: ${message}`);
      return {
        success: false,
        error: message,
        rateLimited,
        retryAfterMs: rateLimited ? 3000 : 1000,
      };
    }
  }

  private stripImagePrefix(base64Image: string) {
    const commaIndex = base64Image.indexOf(',');
    return commaIndex >= 0 ? base64Image.slice(commaIndex + 1) : base64Image;
  }

  private buildLandmarkDebug(landmarks: PoseLandmark[]) {
    const required = [
      ['nose', 0],
      ['leftShoulder', 11],
      ['rightShoulder', 12],
      ['leftElbow', 13],
      ['rightElbow', 14],
      ['leftWrist', 15],
      ['rightWrist', 16],
      ['leftHip', 23],
      ['rightHip', 24],
    ] as const;

    const minConfidence = Number(process.env.POSE_DEBUG_MIN_CONFIDENCE ?? 0.2);
    const scores = Object.fromEntries(required.map(([name, index]) => [name, Number(landmarks[index]?.v ?? 0)]));
    const missing = required
      .filter(([, index]) => Number(landmarks[index]?.v ?? 0) < minConfidence)
      .map(([name]) => name);

    return {
      minConfidence,
      validRequiredCount: required.length - missing.length,
      requiredCount: required.length,
      upperBodyUsable: missing.every((name) => name === 'leftHip' || name === 'rightHip'),
      missing,
      scores,
    };
  }
}
