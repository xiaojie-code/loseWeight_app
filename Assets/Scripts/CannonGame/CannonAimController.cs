using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.CannonGame
{
    /// <summary>
    /// Visual aiming surface for the cannon game.
    /// Keeps all visuals local to the CannonGame canvas.
    /// </summary>
    public class CannonAimController : MonoBehaviour
    {
        private RectTransform _canvasRT;
        private RectTransform _gameAreaRT;
        private RectTransform _cannonRT;
        private RectTransform _barrelRT;
        private RectTransform _crosshairRT;
        private RectTransform _guideRT;
        private Image _crosshairRing;
        private Image _crosshairDot;
        private Image _guideImage;
        private Image _muzzleFlash;
        private Image _damageFlash;
        private Text _inputHintText;
        private readonly List<Image> _explosionPool = new List<Image>();

        private float _aimX = 0.5f;
        private Vector2 _aimCanvasPos;
        private Vector2 _lockedTargetCanvasPos;
        private bool _hasLockedTarget;
        private const float AimY = 0.62f;
        private float _fieldMinX = -420f;
        private float _fieldMaxX = 420f;
        private float _fieldMinY = -500f;
        private float _fieldMaxY = 620f;
        private float _cannonMuzzleY = -520f;
        private Vector2 _lastCanvasSize;
        private Vector2 _lastGameAreaSize;
        private readonly Color _normalColor = new Color(1f, 0.36f, 0.30f, 0.16f);
        private readonly Color _chargeColor = new Color(1f, 0.72f, 0.18f, 0.24f);
        private readonly Color _guideNormalColor = new Color(1f, 0.34f, 0.28f, 0.48f);
        private readonly Color _guideChargeColor = new Color(1f, 0.78f, 0.22f, 0.74f);

        public void Initialize(RectTransform canvas, RectTransform gameArea)
        {
            _canvasRT = canvas;
            _gameAreaRT = gameArea;
            BindSceneNodes();
            RefreshLayout(true);
            SetLockedTarget(false, Vector2.zero);
            UpdateAim(0.5f, AimY);
        }

        public void UpdateAim(float nx, float ny)
        {
            RefreshLayout(false);

            _aimX = Mathf.Clamp01(nx);
            float clampedY = Mathf.Clamp(ny, 0.38f, 0.76f);
            _aimCanvasPos = new Vector2(
                Mathf.Lerp(_fieldMinX, _fieldMaxX, _aimX),
                Mathf.Lerp(_fieldMinY, _fieldMaxY, clampedY));

            UpdateCannonRotation();
            UpdateGuide();
        }

        public Vector2 GetAimScreenPosition()
        {
            return CanvasToScreen(_hasLockedTarget ? _lockedTargetCanvasPos : GetUnlockedGuideEndCanvasPosition());
        }

        public Vector2 GetAimRayEndScreenPosition()
        {
            return CanvasToScreen(GetUnlockedGuideEndCanvasPosition());
        }

        public void SetLockedTarget(bool locked, Vector2 targetScreenPos)
        {
            _hasLockedTarget = locked;
            if (_crosshairRT == null) return;

            _crosshairRT.gameObject.SetActive(locked);
            if (!locked)
            {
                UpdateCannonRotation();
                UpdateGuide();
                return;
            }

            _crosshairRT.anchorMin = new Vector2(0.5f, 0.5f);
            _crosshairRT.anchorMax = new Vector2(0.5f, 0.5f);
            _lockedTargetCanvasPos = ScreenToCanvas(targetScreenPos);
            _crosshairRT.anchoredPosition = _lockedTargetCanvasPos;
            _crosshairRT.localScale = Vector3.one;
            UpdateCannonRotation();
            UpdateGuide();
        }

        public Vector2 GetShotEndScreenPosition()
        {
            Vector2 start = GetCannonScreenPosition();
            Vector2 aim = _hasLockedTarget ? CanvasToScreen(_lockedTargetCanvasPos) : GetAimRayEndScreenPosition();
            Vector2 direction = aim - start;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;

            float distance = Mathf.Max(Screen.width, Screen.height) * 2.4f;
            return start + direction.normalized * distance;
        }

        public Vector2 GetCannonScreenPosition()
        {
            return CanvasToScreen(GetCannonCanvasPosition());
        }

        public void ShowCharge(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (_crosshairRT != null && _crosshairRT.gameObject.activeSelf)
                _crosshairRT.localScale = Vector3.one * Mathf.Lerp(1f, 0.78f, ratio);
            if (_crosshairRing != null)
                _crosshairRing.color = Color.Lerp(_normalColor, _chargeColor, ratio);
            if (_crosshairDot != null)
                _crosshairDot.color = Color.Lerp(Color.white, _chargeColor, ratio);
            if (_guideImage != null)
                _guideImage.color = Color.Lerp(_guideNormalColor, _guideChargeColor, ratio);
        }

        public void ShowInputHint(string text)
        {
            if (_inputHintText != null)
                _inputHintText.text = text;
        }

        public void ShowFireEffect()
        {
            if (_muzzleFlash != null)
            {
                _muzzleFlash.color = new Color(1f, 0.75f, 0.12f, 0.95f);
                StartCoroutine(FadeImage(_muzzleFlash, 0.18f));
            }

            if (_cannonRT != null)
                StartCoroutine(Recoil());

            ShowCharge(0f);
        }

        public void ShowExplosion(Vector2 screenPos, bool hit)
        {
            var explosion = GetAvailableExplosion();
            if (explosion == null)
            {
                Debug.LogWarning("[CannonGame] ExplosionPool exhausted. Increase MCP ExplosionPool size.");
                return;
            }

            var rt = explosion.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(screenPos.x / Mathf.Max(1f, Screen.width), screenPos.y / Mathf.Max(1f, Screen.height));
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = hit ? new Vector2(140f, 140f) : new Vector2(86f, 86f);
            rt.localScale = Vector3.one;
            explosion.color = hit ? new Color(1f, 0.62f, 0.08f, 0.9f) : new Color(0.8f, 0.8f, 0.8f, 0.45f);
            explosion.gameObject.SetActive(true);
            StartCoroutine(FadeAndHide(explosion, 0.26f));
        }

        public void ShowDamageFlash()
        {
            if (_damageFlash == null) return;
            _damageFlash.color = new Color(1f, 0.05f, 0.05f, 0.3f);
            StartCoroutine(FadeImage(_damageFlash, 0.25f));
        }

        private void BindSceneNodes()
        {
            _guideRT = FindRect("AimGuide");
            _guideImage = FindImage("AimGuide");
            _crosshairRT = FindRect("Crosshair");
            _crosshairRing = FindImage("Crosshair");
            _crosshairDot = FindImage("Crosshair/LockVisuals/Dot");
            _cannonRT = FindRect("CannonBase");
            _barrelRT = FindRect("CannonBase/Barrel");
            _muzzleFlash = FindImage("MuzzleFlash");
            _damageFlash = FindImage("DamageFlash");
            _inputHintText = FindText("InputHint");
            BindExplosionPool();
        }

        private void UpdateCannonRotation()
        {
            if (_barrelRT == null) return;
            Vector2 direction = GetUnlockedGuideEndCanvasPosition() - GetCannonCanvasPosition();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            angle = Mathf.Clamp(angle, -56f, 56f);
            _barrelRT.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private void UpdateGuide()
        {
            if (_guideRT == null) return;

            Vector2 start = GetCannonCanvasPosition();
            Vector2 end = _hasLockedTarget ? _lockedTargetCanvasPos : GetUnlockedGuideEndCanvasPosition();
            Vector2 mid = (start + end) * 0.5f;
            float length = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90f;

            _guideRT.anchorMin = new Vector2(0.5f, 0.5f);
            _guideRT.anchorMax = new Vector2(0.5f, 0.5f);
            _guideRT.sizeDelta = new Vector2(Mathf.Clamp(GetCanvasWidth() * 0.008f, 5f, 8f), length);
            _guideRT.anchoredPosition = mid;
            _guideRT.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private Vector2 GetUnlockedGuideEndCanvasPosition()
        {
            Vector2 start = GetCannonCanvasPosition();
            Vector2 direction = _aimCanvasPos - start;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;

            direction.Normalize();
            float t = float.MaxValue;
            if (Mathf.Abs(direction.x) > 0.0001f)
            {
                float tx = ((direction.x > 0f ? _fieldMaxX : _fieldMinX) - start.x) / direction.x;
                if (tx > 0f) t = Mathf.Min(t, tx);
            }
            if (Mathf.Abs(direction.y) > 0.0001f)
            {
                float ty = ((direction.y > 0f ? _fieldMaxY : _fieldMinY) - start.y) / direction.y;
                if (ty > 0f) t = Mathf.Min(t, ty);
            }

            if (float.IsInfinity(t) || t == float.MaxValue)
                t = GetCanvasHeight();

            Vector2 end = start + direction * t;
            end.x = Mathf.Clamp(end.x, _fieldMinX, _fieldMaxX);
            end.y = Mathf.Clamp(end.y, _fieldMinY, _fieldMaxY);
            return end;
        }

        private Vector2 GetCannonCanvasPosition()
        {
            if (_muzzleFlash != null)
                return _muzzleFlash.rectTransform.anchoredPosition;

            if (_cannonRT != null)
                return _cannonRT.anchoredPosition + new Vector2(0f, _cannonRT.rect.height * 0.45f);

            return new Vector2(0f, _cannonMuzzleY);
        }

        private Vector2 CanvasToScreen(Vector2 canvasPos)
        {
            if (_canvasRT == null)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Rect rect = _canvasRT.rect;
            float x = Mathf.InverseLerp(rect.xMin, rect.xMax, canvasPos.x) * Screen.width;
            float y = Mathf.InverseLerp(rect.yMin, rect.yMax, canvasPos.y) * Screen.height;
            return new Vector2(x, y);
        }

        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            if (_canvasRT == null)
                return Vector2.zero;

            Rect rect = _canvasRT.rect;
            float x = Mathf.Lerp(rect.xMin, rect.xMax, screenPos.x / Mathf.Max(1f, Screen.width));
            float y = Mathf.Lerp(rect.yMin, rect.yMax, screenPos.y / Mathf.Max(1f, Screen.height));
            return new Vector2(x, y);
        }

        private void RefreshLayout(bool force)
        {
            if (_canvasRT == null || _gameAreaRT == null) return;

            Vector2 canvasSize = _canvasRT.rect.size;
            Vector2 gameSize = _gameAreaRT.rect.size;
            if (!force
                && Vector2.Distance(canvasSize, _lastCanvasSize) < 0.5f
                && Vector2.Distance(gameSize, _lastGameAreaSize) < 0.5f)
            {
                return;
            }

            _lastCanvasSize = canvasSize;
            _lastGameAreaSize = gameSize;

            Vector3[] worldCorners = new Vector3[4];
            _gameAreaRT.GetWorldCorners(worldCorners);
            Vector2 bottomLeft = _canvasRT.InverseTransformPoint(worldCorners[0]);
            Vector2 topRight = _canvasRT.InverseTransformPoint(worldCorners[2]);

            float width = Mathf.Max(1f, topRight.x - bottomLeft.x);
            float height = Mathf.Max(1f, topRight.y - bottomLeft.y);
            float sideMargin = Mathf.Clamp(width * 0.035f, 16f, 34f);
            float topMargin = Mathf.Clamp(height * 0.055f, 40f, 72f);
            float bottomMargin = Mathf.Clamp(height * 0.075f, 46f, 86f);

            _fieldMinX = bottomLeft.x + sideMargin;
            _fieldMaxX = topRight.x - sideMargin;
            _fieldMinY = bottomLeft.y + bottomMargin + height * 0.18f;
            _fieldMaxY = topRight.y - topMargin;
            _cannonMuzzleY = bottomLeft.y + Mathf.Clamp(height * 0.08f, 46f, 78f);
        }

        private float GetCanvasWidth()
        {
            return _canvasRT != null ? Mathf.Max(1f, _canvasRT.rect.width) : 1080f;
        }

        private float GetCanvasHeight()
        {
            return _canvasRT != null ? Mathf.Max(1f, _canvasRT.rect.height) : 1920f;
        }

        private IEnumerator Recoil()
        {
            Vector3 start = _cannonRT.localScale;
            _cannonRT.localScale = new Vector3(1.08f, 0.92f, 1f);
            yield return new WaitForSeconds(0.08f);
            _cannonRT.localScale = start;
        }

        private IEnumerator FadeImage(Image image, float duration)
        {
            Color start = image.color;
            float elapsed = 0f;
            while (elapsed < duration && image != null)
            {
                elapsed += Time.deltaTime;
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, elapsed / duration);
                image.color = c;
                yield return null;
            }
            if (image != null) image.color = Color.clear;
        }

        private IEnumerator FadeAndHide(Image image, float duration)
        {
            yield return FadeImage(image, duration);
            if (image != null) image.gameObject.SetActive(false);
        }

        private void BindExplosionPool()
        {
            _explosionPool.Clear();
            var root = FindChild(_canvasRT, "ExplosionPool");
            if (root == null) return;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name.StartsWith("Explosion_"))
                {
                    image.gameObject.SetActive(false);
                    _explosionPool.Add(image);
                }
            }
        }

        private Image GetAvailableExplosion()
        {
            foreach (var image in _explosionPool)
            {
                if (image != null && !image.gameObject.activeInHierarchy)
                    return image;
            }
            return null;
        }

        private RectTransform FindRect(string relativePath)
        {
            var child = _canvasRT != null ? FindChild(_canvasRT, relativePath) : null;
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        private Image FindImage(string relativePath)
        {
            var child = _canvasRT != null ? FindChild(_canvasRT, relativePath) : null;
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string relativePath)
        {
            var child = _canvasRT != null ? FindChild(_canvasRT, relativePath) : null;
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Transform FindChild(Transform root, string relativePath)
        {
            if (root == null) return null;
            var direct = root.Find(relativePath);
            if (direct != null) return direct;
            string name = relativePath.Contains("/") ? relativePath.Substring(relativePath.LastIndexOf('/') + 1) : relativePath;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
