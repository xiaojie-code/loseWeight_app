// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using LoseWeight.PoseDetection;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
  public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
  {
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;

    private Experimental.TextureFramePool _textureFramePool;

    public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Model = {config.ModelName}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumPoses = {config.NumPoses}");
      Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
      Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
      Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
      Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");
      LoseWeight.Game.ScreenDebugLog.Log($"MP: delegate={config.Delegate} model={config.ModelName}");

      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      var options = config.GetPoseLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);

      // CPU \u6a21\u5f0f\u4e0d\u4f7f\u7528 GpuResources
      GpuResources gpuRes = null;
#if !UNITY_EDITOR
      // \u771f\u673a\u4e0a\u5982\u679c\u662f CPU \u6a21\u5f0f\uff0c\u4e0d\u4f20 GpuResources
      if (config.Delegate != Tasks.Core.BaseOptions.Delegate.CPU)
        gpuRes = GpuManager.GpuResources;
#else
      gpuRes = GpuManager.GpuResources;
#endif

      try
      {
        taskApi = PoseLandmarker.CreateFromOptions(options, gpuRes);
        Debug.Log($"[PoseLandmarker] Created successfully, delegate={config.Delegate}");
        LoseWeight.Game.ScreenDebugLog.Log("MP: CreateFromOptions OK");
      }
      catch (System.Exception e)
      {
        Debug.LogError($"[PoseLandmarker] CreateFromOptions FAILED: {e.Message}");
        LoseWeight.Game.ScreenDebugLog.Log($"MP FAIL: {e.Message.Substring(0, Mathf.Min(50, e.Message.Length))}");
        yield break;
      }

      var imageSource = ImageSourceProvider.ImageSource;
      LoseWeight.Game.ScreenDebugLog.Log("MP: starting imageSource...");

      // Android 上 Bootstrap 可能没有初始化 ImageSource，手动初始化
      if (imageSource == null && Application.platform == RuntimePlatform.Android)
      {
        LoseWeight.Game.ScreenDebugLog.Log("MP: ImageSource null, init manually");
        var appSettings = Resources.FindObjectsOfTypeAll<AppSettings>();
        if (appSettings != null && appSettings.Length > 0)
        {
          ImageSourceProvider.Initialize(
            appSettings[0].BuildWebCamSource(),
            appSettings[0].BuildStaticImageSource(),
            appSettings[0].BuildVideoSource());
          ImageSourceProvider.Switch(appSettings[0].defaultImageSource);
          imageSource = ImageSourceProvider.ImageSource;
          LoseWeight.Game.ScreenDebugLog.Log($"MP: ImageSource init OK: {imageSource != null}");
        }
        else
        {
          LoseWeight.Game.ScreenDebugLog.Log("MP: AppSettings not found!");
          yield break;
        }
      }

      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        LoseWeight.Game.ScreenDebugLog.Log("MP: ImageSource FAILED");
        yield break;
      }
      LoseWeight.Game.ScreenDebugLog.Log($"MP: cam {imageSource.textureWidth}x{imageSource.textureHeight}");

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);

      SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
      _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;

      // Always setting rotationDegrees to 0 to avoid the issue that the detection becomes unstable when the input image is rotated.
      // https://github.com/homuler/MediaPipeUnityPlugin/issues/1196
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();
      var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // Build the input Image
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              throw new System.Exception("ImageReadMode.GPU is not supported");
            }
            textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
            yield return waitForEndOfFrame;
            break;
          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
          case ImageReadMode.CPUAsync:
          default:
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
              Debug.LogWarning($"Failed to read texture from the image source");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:
            if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
            {
              _poseLandmarkerResultAnnotationController.DrawNow(result);
              PoseEventBridge.Publish(result);
            }
            else
            {
              _poseLandmarkerResultAnnotationController.DrawNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.VIDEO:
            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
            {
              _poseLandmarkerResultAnnotationController.DrawNow(result);
              PoseEventBridge.Publish(result);
            }
            else
            {
              _poseLandmarkerResultAnnotationController.DrawNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }
      }
    }

    private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
    {
      _poseLandmarkerResultAnnotationController.DrawLater(result);
      PoseEventBridge.Publish(result);
      DisposeAllMasks(result);
    }

    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
      if (result.segmentationMasks != null)
      {
        foreach (var mask in result.segmentationMasks)
        {
          mask.Dispose();
        }
      }
    }
  }
}
