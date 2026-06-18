using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.CannonGame
{
    public enum ZombieType { Normal, Fast, Tank, Bomber, Boss }

    public class Zombie : MonoBehaviour
    {
        public float Size { get; private set; }
        public bool IsDead { get; private set; }
        public ZombieType Type => _type;
        public System.Action<Zombie> OnReachedBottom;

        private RectTransform _rt;
        private Image _body;
        private Image _head;
        private Image _hpFill;
        private Text _label;
        private ZombieType _type;
        private int _hp;
        private int _maxHp;
        private int _scoreValue;
        private float _speed;
        private float _wobbleOffset;
        private float _deathTime;
        private bool _reachedBottom;
        private Vector2 _basePos;
        private Color _baseColor;

        public void Initialize(Vector2 position, ZombieType type, float speedMultiplier)
        {
            _rt = GetComponent<RectTransform>();
            if (_rt != null)
            {
                _rt.anchorMin = Vector2.zero;
                _rt.anchorMax = Vector2.zero;
                _rt.pivot = new Vector2(0.5f, 0.5f);
            }

            _type = type;
            _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
            _basePos = position;
            _reachedBottom = false;
            IsDead = false;

            Configure(type, speedMultiplier);
            BindVisuals();
            UpdateHpBar();

            _rt.anchoredPosition = position;
            _rt.sizeDelta = new Vector2(Size, Size * 1.25f);
            _rt.localScale = Vector3.one;
            gameObject.name = type.ToString();
        }

        public int TakeHit()
        {
            if (IsDead) return 0;

            _hp--;
            if (_hp <= 0)
            {
                Kill();
                return _scoreValue;
            }

            UpdateHpBar();
            if (_body != null) _body.color = Color.Lerp(Color.red, _baseColor, 0.45f);
            _speed *= 0.82f;
            return 0;
        }

        private void Update()
        {
            if (IsDead)
            {
                float t = Mathf.Clamp01((Time.time - _deathTime) / 0.35f);
                _rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.35f, t);
                if (_body != null) _body.color = Fade(_body.color, 1f - t);
                if (_head != null) _head.color = Fade(_head.color, 1f - t);
                if (t >= 1f) Despawn();
                return;
            }

            _basePos.y -= _speed * Time.deltaTime;
            float wobble = Mathf.Sin((Time.time + _wobbleOffset) * 3.2f) * GetWobbleAmount();
            _rt.anchoredPosition = _basePos + new Vector2(wobble, 0f);

            var parent = _rt.parent as RectTransform;
            if (parent != null && _basePos.y < parent.rect.height * 0.18f)
            {
                float blink = Mathf.Sin(Time.time * 14f) > 0 ? 1f : 0.25f;
                if (_body != null) _body.color = Color.Lerp(_baseColor, Color.red, blink * 0.45f);
            }

            if (_basePos.y < -Size && !_reachedBottom)
            {
                _reachedBottom = true;
                OnReachedBottom?.Invoke(this);
                Despawn();
            }
        }

        private void Configure(ZombieType type, float speedMultiplier)
        {
            switch (type)
            {
                case ZombieType.Fast:
                    _hp = _maxHp = 1;
                    _scoreValue = 140;
                    _speed = 170f;
                    Size = 66f;
                    _baseColor = new Color(0.38f, 0.78f, 0.9f);
                    break;
                case ZombieType.Tank:
                    _hp = _maxHp = 3;
                    _scoreValue = 260;
                    _speed = 58f;
                    Size = 112f;
                    _baseColor = new Color(0.38f, 0.62f, 0.38f);
                    break;
                case ZombieType.Bomber:
                    _hp = _maxHp = 2;
                    _scoreValue = 220;
                    _speed = 92f;
                    Size = 88f;
                    _baseColor = new Color(0.78f, 0.42f, 0.22f);
                    break;
                case ZombieType.Boss:
                    _hp = _maxHp = 6;
                    _scoreValue = 600;
                    _speed = 42f;
                    Size = 138f;
                    _baseColor = new Color(0.66f, 0.24f, 0.2f);
                    break;
                default:
                    _hp = _maxHp = 1;
                    _scoreValue = 100;
                    _speed = 96f;
                    Size = 78f;
                    _baseColor = new Color(0.52f, 0.72f, 0.42f);
                    break;
            }

            _speed *= speedMultiplier;
        }

        private void BindVisuals()
        {
            _body = FindImage("Body");
            _head = FindImage("Head");
            _hpFill = FindImage("HpFill");
            _label = FindText("Label");

            var leftEye = FindImage("LeftEye");
            var rightEye = FindImage("RightEye");
            var hpBg = FindImage("HpBg");

            if (_body != null) _body.color = _baseColor;
            if (_head != null) _head.color = Color.Lerp(_baseColor, Color.white, 0.15f);
            if (leftEye != null) leftEye.color = new Color(1f, 0.9f, 0.25f, 1f);
            if (rightEye != null) rightEye.color = new Color(1f, 0.9f, 0.25f, 1f);
            if (hpBg != null) hpBg.color = new Color(0f, 0f, 0f, 0.55f);
            if (_label != null) _label.text = GetLabel(_type);
        }

        private void UpdateHpBar()
        {
            if (_hpFill == null) return;
            float ratio = Mathf.Clamp01((float)_hp / _maxHp);
            var rt = _hpFill.rectTransform;
            rt.anchorMax = new Vector2(0.2f + 0.6f * ratio, rt.anchorMax.y);
            _hpFill.color = Color.Lerp(Color.red, new Color(0.1f, 0.95f, 0.25f), ratio);
        }

        private void Kill()
        {
            IsDead = true;
            _deathTime = Time.time;
            if (_label != null) _label.text = "";
        }

        public void Despawn()
        {
            IsDead = true;
            OnReachedBottom = null;
            gameObject.SetActive(false);
        }

        private float GetWobbleAmount()
        {
            return _type == ZombieType.Fast ? 5f : _type == ZombieType.Boss ? 2f : 3f;
        }

        private string GetLabel(ZombieType type)
        {
            switch (type)
            {
                case ZombieType.Fast: return "<快速僵尸>";
                case ZombieType.Tank: return "<坦克僵尸>";
                case ZombieType.Bomber: return "<爆破僵尸>";
                case ZombieType.Boss: return "<狂暴僵尸>";
                default: return "<普通僵尸>";
            }
        }

        private Color Fade(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private Image FindImage(string name)
        {
            var child = transform.Find(name);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string name)
        {
            var child = transform.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }
    }
}
