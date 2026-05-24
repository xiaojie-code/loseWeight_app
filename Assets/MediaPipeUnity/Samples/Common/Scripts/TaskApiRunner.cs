// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace Mediapipe.Unity.Sample
{
  public abstract class BaseRunner : MonoBehaviour
  {
    private static readonly string _BootstrapName = nameof(Bootstrap);

    [SerializeField] private GameObject _bootstrapPrefab;

#pragma warning disable IDE1006
    // TODO: make it static
    protected virtual string TAG => GetType().Name;
#pragma warning restore IDE1006

    protected Bootstrap bootstrap;
    protected bool isPaused;

    private readonly Stopwatch _stopwatch = new();

    protected virtual IEnumerator Start()
    {
      bootstrap = FindBootstrap();
      Debug.Log("[BaseRunner] Waiting for Bootstrap...");

      // Android 真机上 Bootstrap 的原生初始化（Protobuf/Glog）会卡死
      // 直接跳过等待，让 PoseLandmarkerRunner 自己处理初始化失败
      if (Application.platform == RuntimePlatform.Android)
      {
        Debug.Log("[BaseRunner] Android: skipping Bootstrap wait");
        LoseWeight.Game.ScreenDebugLog.Log("BaseRunner: skip Bootstrap (Android)");
        yield return new WaitForSeconds(1f);
        Play();
        yield break;
      }

      float waited = 0f;
      while (!bootstrap.isFinished)
      {
        yield return new WaitForSeconds(0.5f);
        waited += 0.5f;
        if (waited >= 2f && (int)(waited * 2) % 4 == 0)
        {
          Debug.Log($"[BaseRunner] Still waiting for Bootstrap... {waited:F0}s");
          LoseWeight.Game.ScreenDebugLog.Log($"Bootstrap wait: {waited:F0}s");
        }
        if (waited > 30f)
        {
          Debug.LogError("[BaseRunner] Bootstrap timeout after 30s, giving up");
          LoseWeight.Game.ScreenDebugLog.Log("Bootstrap TIMEOUT");
          yield break;
        }
      }

      Debug.Log("[BaseRunner] Bootstrap finished, calling Play()");
      LoseWeight.Game.ScreenDebugLog.Log("Bootstrap OK, starting MP");
      Play();
    }

    /// <summary>
    ///   Start the main program from the beginning.
    /// </summary>
    public virtual void Play()
    {
      isPaused = false;
      _stopwatch.Restart();
    }

    /// <summary>
    ///   Pause the main program.
    /// <summary>
    public virtual void Pause()
    {
      isPaused = true;
    }

    /// <summary>
    ///    Resume the main program.
    ///    If the main program has not begun, it'll do nothing.
    /// </summary>
    public virtual void Resume()
    {
      isPaused = false;
    }

    /// <summary>
    ///   Stops the main program.
    /// </summary>
    public virtual void Stop()
    {
      isPaused = true;
      _stopwatch.Stop();
    }

    protected long GetCurrentTimestampMillisec() => _stopwatch.IsRunning ? _stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond : -1;

    protected Bootstrap FindBootstrap()
    {
      var bootstrapObj = GameObject.Find("Bootstrap");

      if (bootstrapObj == null)
      {
        Debug.Log("Initializing the Bootstrap GameObject");
        bootstrapObj = Instantiate(_bootstrapPrefab);
        bootstrapObj.name = _BootstrapName;
        DontDestroyOnLoad(bootstrapObj);
      }

      return bootstrapObj.GetComponent<Bootstrap>();
    }
  }
}
