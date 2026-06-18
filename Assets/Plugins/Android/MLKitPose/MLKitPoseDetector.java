package com.ycykj.mlkit;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.util.Log;

import com.google.mlkit.vision.common.InputImage;
import com.google.mlkit.vision.pose.Pose;
import com.google.mlkit.vision.pose.PoseDetection;
import com.google.mlkit.vision.pose.PoseDetector;
import com.google.mlkit.vision.pose.PoseLandmark;
import com.google.mlkit.vision.pose.defaults.PoseDetectorOptions;

import com.unity3d.player.UnityPlayer;

/**
 * ML Kit Pose Detection 封装
 * Unity 通过 detectFromBytes 传入摄像头画面，结果通过 UnitySendMessage 异步回调
 *
 * 输出 JSON 示例：
 *   {"landmarks":[{"x":0.5,"y":0.5,"z":0.0,"v":0.95},...]}
 */
public class MLKitPoseDetector {
    private static final String TAG = "MLKitPose";
    private static MLKitPoseDetector sInstance;

    private PoseDetector mDetector;
    private String mUnityCallbackTarget = "PoseSystem";
    private String mUnityCallbackMethod = "OnMLKitPoseResult";
    private boolean mProcessing = false;
    private long mFrameCount = 0;

    public static synchronized MLKitPoseDetector getInstance() {
        if (sInstance == null) sInstance = new MLKitPoseDetector();
        return sInstance;
    }

    private MLKitPoseDetector() {
        // STREAM_MODE: 视频流模式，速度最快，单人
        PoseDetectorOptions options = new PoseDetectorOptions.Builder()
                .setDetectorMode(PoseDetectorOptions.STREAM_MODE)
                .build();
        mDetector = PoseDetection.getClient(options);
        Log.i(TAG, "MLKit PoseDetector initialized (STREAM_MODE)");
    }

    public void setUnityCallback(String target, String method) {
        mUnityCallbackTarget = target;
        mUnityCallbackMethod = method;
    }

    /**
     * 检测姿态 - 接收 RGBA 像素数组
     * @param rgba RGBA 字节数组（每像素 4 字节）
     * @param width 图像宽度
     * @param height 图像高度
     * @param rotation 图像旋转角度（0/90/180/270）
     */
    public void detectFromRgba(byte[] rgba, int width, int height, int rotation) {
        if (mProcessing) return; // 上一帧还没处理完，丢弃
        if (rgba == null || width <= 0 || height <= 0) {
            sendErrorToUnity("Invalid RGBA frame");
            return;
        }
        if (mDetector == null) {
            sendErrorToUnity("Detector is not initialized");
            return;
        }

        mProcessing = true;
        try {
            Bitmap bmp = rgbaToBitmap(rgba, width, height);
            if (bmp == null) {
                mProcessing = false;
                sendErrorToUnity("Failed to build bitmap from RGBA frame");
                return;
            }

            InputImage image = InputImage.fromBitmap(bmp, rotation);
            final int w = width;
            final int h = height;
            mDetector.process(image)
                    .addOnSuccessListener(pose -> {
                        String json = poseToJson(pose, w, h);
                        sendToUnity(json);
                        bmp.recycle();
                        mProcessing = false;
                    })
                    .addOnFailureListener(e -> {
                        Log.w(TAG, "Pose detect failed: " + e.getMessage());
                        bmp.recycle();
                        mProcessing = false;
                        sendErrorToUnity("Pose detect failed: " + e.getMessage());
                    });
        } catch (Exception e) {
            Log.e(TAG, "detectFromRgba error: " + e.getMessage());
            mProcessing = false;
            sendErrorToUnity("detectFromRgba error: " + e.getMessage());
        }
    }

    /**
     * 检测姿态 - 接收 JPEG 字节数组
     */
    public void detectFromJpeg(byte[] jpeg, int rotation) {
        if (mProcessing) return;
        if (jpeg == null || jpeg.length == 0) {
            sendErrorToUnity("Invalid JPEG frame");
            return;
        }
        if (mDetector == null) {
            sendErrorToUnity("Detector is not initialized");
            return;
        }

        mProcessing = true;
        try {
            Bitmap bmp = BitmapFactory.decodeByteArray(jpeg, 0, jpeg.length);
            if (bmp == null) {
                mProcessing = false;
                sendErrorToUnity("Failed to decode JPEG frame");
                return;
            }
            final int w = bmp.getWidth();
            final int h = bmp.getHeight();
            InputImage image = InputImage.fromBitmap(bmp, rotation);
            mDetector.process(image)
                    .addOnSuccessListener(pose -> {
                        String json = poseToJson(pose, w, h);
                        sendToUnity(json);
                        bmp.recycle();
                        mProcessing = false;
                    })
                    .addOnFailureListener(e -> {
                        Log.w(TAG, "Pose detect failed: " + e.getMessage());
                        bmp.recycle();
                        mProcessing = false;
                        sendErrorToUnity("Pose detect failed: " + e.getMessage());
                    });
        } catch (Exception e) {
            Log.e(TAG, "detectFromJpeg error: " + e.getMessage());
            mProcessing = false;
            sendErrorToUnity("detectFromJpeg error: " + e.getMessage());
        }
    }

    public void close() {
        try {
            if (mDetector != null) {
                mDetector.close();
                mDetector = null;
            }
        } catch (Exception ignored) {}
    }

    /**
     * 重新初始化 PoseDetector（用于再玩一局时）
     */
    public void reinit() {
        close();
        try {
            PoseDetectorOptions options = new PoseDetectorOptions.Builder()
                    .setDetectorMode(PoseDetectorOptions.STREAM_MODE)
                    .build();
            mDetector = PoseDetection.getClient(options);
            mProcessing = false;
            mFrameCount = 0;
            Log.i(TAG, "MLKit PoseDetector re-initialized");
        } catch (Exception e) {
            Log.e(TAG, "reinit failed: " + e.getMessage());
        }
    }

    // ====================== 工具方法 ======================

    private Bitmap rgbaToBitmap(byte[] rgba, int width, int height) {
        if (rgba.length < width * height * 4) return null;
        int[] pixels = new int[width * height];
        for (int i = 0; i < pixels.length; i++) {
            int r = rgba[i * 4] & 0xff;
            int g = rgba[i * 4 + 1] & 0xff;
            int b = rgba[i * 4 + 2] & 0xff;
            int a = rgba[i * 4 + 3] & 0xff;
            pixels[i] = Color.argb(a, r, g, b);
        }
        return Bitmap.createBitmap(pixels, width, height, Bitmap.Config.ARGB_8888);
    }

    /**
     * ML Kit 33 个关键点 → JSON
     * 索引和 MediaPipe 一致（PoseLandmark.NOSE = 0, LEFT_SHOULDER = 11, ...）
     * 输出的 x/y 已归一化到 [0, 1]，z 保持原始值
     */
    private String poseToJson(Pose pose, int imageWidth, int imageHeight) {
        StringBuilder sb = new StringBuilder("{\"landmarks\":[");
        float invW = imageWidth > 0 ? 1f / imageWidth : 1f;
        float invH = imageHeight > 0 ? 1f / imageHeight : 1f;
        for (int i = 0; i <= 32; i++) {
            PoseLandmark lm = pose.getPoseLandmark(i);
            if (i > 0) sb.append(",");
            if (lm == null) {
                sb.append("{\"x\":0,\"y\":0,\"z\":0,\"v\":0}");
            } else {
                float nx = lm.getPosition3D().getX() * invW;
                float ny = lm.getPosition3D().getY() * invH;
                sb.append("{\"x\":").append(nx)
                  .append(",\"y\":").append(ny)
                  .append(",\"z\":").append(lm.getPosition3D().getZ() * invW)
                  .append(",\"v\":").append(lm.getInFrameLikelihood())
                  .append("}");
            }
        }
        sb.append("]}");
        return sb.toString();
    }

    private void sendErrorToUnity(String message) {
        sendToUnity("{\"error\":\"" + escapeJson(message) + "\",\"landmarks\":[]}");
    }

    private String escapeJson(String value) {
        if (value == null) return "";
        return value
                .replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\n", " ")
                .replace("\r", " ");
    }

    private void sendToUnity(String json) {
        try {
            UnityPlayer.UnitySendMessage(mUnityCallbackTarget, mUnityCallbackMethod, json);
        } catch (Throwable t) {
            Log.e(TAG, "UnitySendMessage failed: " + t.getMessage());
        }
    }
}
