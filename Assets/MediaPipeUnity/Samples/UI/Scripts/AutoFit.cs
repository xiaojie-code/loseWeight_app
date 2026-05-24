// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using UnityEngine;

namespace Mediapipe.Unity
{
  public class AutoFit : MonoBehaviour
  {
    [System.Serializable]
    public enum FitMode
    {
      Expand,
      Shrink,
      FitWidth,
      FitHeight,
      Fixed,
    }

    [SerializeField] private FitMode _fitMode;

    [Tooltip("Fixed size used when FitMode is Fixed")]
    [SerializeField] private Vector2 _fixedSize = new Vector2(150, 200);

    public bool IsFixed => _fitMode == FitMode.Fixed;

    // 记录原始sizeDelta，避免每帧累积缩放
    private Vector2 _lastSizeDelta;
    private bool _fitted;

    private void LateUpdate()
    {
      var rectTransform = GetComponent<RectTransform>();

      if (_fitMode == FitMode.Fixed)
      {
        rectTransform.sizeDelta = _fixedSize;
        return;
      }

      if (rectTransform.rect.width == 0 || rectTransform.rect.height == 0)
      {
        _fitted = false;
        return;
      }

      // 如果sizeDelta没变化且已经fit过，就不再重复fit
      if (_fitted && rectTransform.sizeDelta == _lastSizeDelta)
      {
        return;
      }

      var parentRect = gameObject.transform.parent.gameObject.GetComponent<RectTransform>().rect;
      if (parentRect.width == 0 || parentRect.height == 0)
      {
        return;
      }

      var (width, height) = GetBoundingBoxSize(rectTransform);

      var ratio = parentRect.width / width;
      var h = height * ratio;

      if (_fitMode == FitMode.FitWidth || (_fitMode == FitMode.Expand && h >= parentRect.height) || (_fitMode == FitMode.Shrink && h <= parentRect.height))
      {
        rectTransform.offsetMin *= ratio;
        rectTransform.offsetMax *= ratio;
        _lastSizeDelta = rectTransform.sizeDelta;
        _fitted = true;
        return;
      }

      ratio = parentRect.height / height;

      rectTransform.offsetMin *= ratio;
      rectTransform.offsetMax *= ratio;
      _lastSizeDelta = rectTransform.sizeDelta;
      _fitted = true;
    }

    // 当外部改变了sizeDelta时（比如Screen.Resize），需要重新fit
    public void RequestRefit()
    {
      _fitted = false;
    }

    private (float, float) GetBoundingBoxSize(RectTransform rectTransform)
    {
      var rect = rectTransform.rect;
      var center = rect.center;
      var topLeftRel = new Vector2(rect.xMin - center.x, rect.yMin - center.y);
      var topRightRel = new Vector2(rect.xMax - center.x, rect.yMin - center.y);
      var rotatedTopLeftRel = rectTransform.localRotation * topLeftRel;
      var rotatedTopRightRel = rectTransform.localRotation * topRightRel;
      var wMax = Mathf.Max(Mathf.Abs(rotatedTopLeftRel.x), Mathf.Abs(rotatedTopRightRel.x));
      var hMax = Mathf.Max(Mathf.Abs(rotatedTopLeftRel.y), Mathf.Abs(rotatedTopRightRel.y));
      return (2 * wMax, 2 * hMax);
    }
  }
}
