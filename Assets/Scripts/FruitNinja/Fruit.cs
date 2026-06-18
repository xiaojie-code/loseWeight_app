using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.FruitNinja
{
    public class Fruit : MonoBehaviour
    {
        public bool IsBomb;
        public int Score = 10;
        public float CollisionRadius = 70f;
        public System.Action OnMissed;

        private Vector2 _velocity;
        private float _gravity;
        private RectTransform _rt;
        private Image _image;
        private bool _sliced;
        private float _despawnTime;
        private bool _missNotified;

        public bool IsSliced => _sliced;

        public void Initialize(Vector2 startPos, Vector2 velocity, float gravity, Color color, bool isBomb)
        {
            _rt = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            if (_rt == null || _image == null)
            {
                Debug.LogError("[FruitNinja] Fruit pool object missing RectTransform/Image.");
                gameObject.SetActive(false);
                return;
            }

            _rt.anchoredPosition = startPos;
            _rt.localScale = Vector3.one;
            _velocity = velocity;
            _gravity = gravity;
            IsBomb = isBomb;
            _sliced = false;
            _missNotified = false;
            _image.color = color;
            _rt.sizeDelta = isBomb ? new Vector2(90, 90) : new Vector2(80, 80);
        }

        private void Update()
        {
            if (_sliced)
            {
                if (Time.time > _despawnTime) Despawn();
                return;
            }

            _velocity.y -= _gravity * Time.deltaTime;
            _rt.anchoredPosition += _velocity * Time.deltaTime;

            if (_rt.anchoredPosition.y < -150f)
            {
                if (!IsBomb && !_missNotified)
                {
                    _missNotified = true;
                    OnMissed?.Invoke();
                }
                Despawn();
            }
        }

        public Vector2 GetScreenPosition()
        {
            return _rt != null ? _rt.anchoredPosition : Vector2.zero;
        }

        public void Slice()
        {
            if (_sliced) return;
            _sliced = true;
            _despawnTime = Time.time + 0.3f;
            if (_image != null) _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0.3f);
            if (_rt != null) _rt.localScale = Vector3.one * 0.4f;
            _velocity = new Vector2(Random.Range(-100f, 100f), Random.Range(-50f, 50f));
            _gravity = 0;
        }

        public void Despawn()
        {
            OnMissed = null;
            gameObject.SetActive(false);
        }
    }
}
