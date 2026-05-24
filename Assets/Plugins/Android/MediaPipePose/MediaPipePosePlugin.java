package com.loseweight.pose;

import android.app.Activity;
import android.util.Log;
import com.unity3d.player.UnityPlayer;

/**
 * Android 原生 MediaPipe 姿态识别插件
 * 负责：摄像头管理 + MediaPipe 推理 + 关键点回传 Unity
 *
 * Unity 通过 AndroidJavaClass 调用此类的静态方法
 * 推理结果通过 UnitySendMessage 回传
 */
public class MediaPipePosePlugin {

    private static final String TAG = "MediaPipePose";
    private static final String UNITY_OBJECT = "PoseProvider";

    private static PoseDetectionManager sManager;

    /**
     * 启动姿态检测
     * @param activity Unity Activity
     * @param targetFps 目标帧率
     * @param useFrontCamera 是否使用前置摄像头
     */
    public static void startPoseDetection(Activity activity, int targetFps, boolean useFrontCamera) {
        Log.d(TAG, "startPoseDetection: fps=" + targetFps + ", front=" + useFrontCamera);

        if (sManager != null) {
            sManager.stop();
        }

        sManager = new PoseDetectionManager(activity, targetFps, useFrontCamera, new PoseDetectionManager.PoseCallback() {
            @Override
            public void onPoseResult(String jsonLandmarks) {
                // 回传到 Unity 主线程
                UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnNativePoseData", jsonLandmarks);
            }

            @Override
            public void onError(String error) {
                UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnNativePoseError", error);
            }
        });

        sManager.start();
    }

    /**
     * 停止姿态检测
     */
    public static void stopPoseDetection() {
        Log.d(TAG, "stopPoseDetection");
        if (sManager != null) {
            sManager.stop();
            sManager = null;
        }
    }

    /**
     * 检查是否正在运行
     */
    public static boolean isRunning() {
        return sManager != null && sManager.isRunning();
    }
}
