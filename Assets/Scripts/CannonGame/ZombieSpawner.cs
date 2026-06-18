using System.Collections.Generic;
using UnityEngine;

namespace LoseWeight.CannonGame
{
    public class ZombieSpawner : MonoBehaviour
    {
        private readonly List<Zombie> _activeZombies = new List<Zombie>();
        private readonly List<Zombie> _zombiePool = new List<Zombie>();
        private RectTransform _spawnArea;
        private bool _isSpawning;
        private float _nextSpawnTime;
        private float _elapsed;
        private int _wave = 1;
        private int _stage = 1;
        private const float NearTargetBackExtendScreenRatio = 0.34f;
        private const float NearBottomTargetYRatio = 0.42f;
        private const float NearBottomCenterLaneRatio = 0.28f;

        public int Wave => _wave;
        public int Stage => _stage;
        public System.Action<Zombie> OnZombieReachedBottom;
        public System.Action<int, int> OnWaveChanged;

        public void Initialize(RectTransform area)
        {
            _spawnArea = area;
            BindPool();
        }

        public void StartSpawning()
        {
            _isSpawning = true;
            _elapsed = 0f;
            _wave = 1;
            _stage = 1;
            _nextSpawnTime = Time.time + 0.7f;
            _activeZombies.Clear();
            OnWaveChanged?.Invoke(_wave, _stage);
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            foreach (var zombie in _activeZombies)
            {
                if (zombie != null) zombie.Despawn();
            }
            _activeZombies.Clear();
        }

        private void Update()
        {
            if (!_isSpawning || _spawnArea == null) return;

            _elapsed += Time.deltaTime;
            int newWave = Mathf.Clamp(1 + Mathf.FloorToInt(_elapsed / 12f), 1, 6);
            int newStage = 1 + Mathf.FloorToInt(_elapsed / 24f);
            if (newWave != _wave || newStage != _stage)
            {
                _wave = newWave;
                _stage = newStage;
                OnWaveChanged?.Invoke(_wave, _stage);
            }

            if (Time.time >= _nextSpawnTime)
            {
                SpawnZombie(GetZombieType());
                _nextSpawnTime = Time.time + GetSpawnInterval();

                if (_wave >= 3 && Random.value < 0.32f) SpawnZombie(GetZombieType());
                if (_wave >= 5 && Random.value < 0.22f) SpawnZombie(GetZombieType());
            }

            _activeZombies.RemoveAll(z => z == null || !z.gameObject.activeInHierarchy);
        }

        public bool TryHitAlongShot(Vector2 startScreen, Vector2 endScreen, float hitWidth, out Vector2 hitPos, out int score)
        {
            score = 0;
            Zombie closest = FindClosestTargetAlongShot(startScreen, endScreen, hitWidth, out hitPos);
            if (closest == null) return false;

            score = closest.TakeHit();
            return true;
        }

        public bool TryFindTargetAlongShot(Vector2 startScreen, Vector2 endScreen, float lockWidth, out Vector2 targetPos)
        {
            return FindClosestTargetAlongShot(startScreen, endScreen, lockWidth, out targetPos) != null;
        }

        private Zombie FindClosestTargetAlongShot(Vector2 startScreen, Vector2 endScreen, float hitWidth, out Vector2 hitPos)
        {
            _activeZombies.RemoveAll(z => z == null || !z.gameObject.activeInHierarchy);
            Zombie closest = null;
            float closestProgress = float.MaxValue;
            hitPos = endScreen;
            Vector2 shotDirection = endScreen - startScreen;
            if (shotDirection.sqrMagnitude < 0.001f)
                shotDirection = Vector2.up;

            shotDirection.Normalize();
            Vector2 extendedStart = startScreen - shotDirection * (Mathf.Max(Screen.width, Screen.height) * NearTargetBackExtendScreenRatio);

            foreach (var zombie in _activeZombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                Vector2 zombieScreen = zombie.GetComponent<RectTransform>().position;
                if (!IsScreenPointVisible(zombieScreen)) continue;

                float progress;
                float distance = DistanceToSegment(zombieScreen, extendedStart, endScreen, out progress);
                float allowed = hitWidth + zombie.Size * 0.46f;
                if (distance <= allowed && progress < closestProgress)
                {
                    closest = zombie;
                    closestProgress = progress;
                    hitPos = zombieScreen;
                }
            }

            if (closest != null)
                return closest;

            return FindNearBottomTarget(startScreen, shotDirection, out hitPos);
        }

        private Zombie FindNearBottomTarget(Vector2 startScreen, Vector2 shotDirection, out Vector2 hitPos)
        {
            Zombie best = null;
            float bestScore = float.MaxValue;
            hitPos = startScreen;
            float bottomLimit = Screen.height * NearBottomTargetYRatio;
            float centerLaneWidth = Screen.width * NearBottomCenterLaneRatio;

            foreach (var zombie in _activeZombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                Vector2 zombieScreen = zombie.GetComponent<RectTransform>().position;
                if (!IsScreenPointVisible(zombieScreen)) continue;
                if (zombieScreen.y > bottomLimit) continue;

                float horizontal = zombieScreen.x - startScreen.x;
                if (Mathf.Abs(shotDirection.x) > 0.075f)
                {
                    if (Mathf.Sign(horizontal) != Mathf.Sign(shotDirection.x))
                        continue;
                }
                else if (Mathf.Abs(horizontal) > centerLaneWidth)
                {
                    continue;
                }

                float distanceFromCannon = Vector2.Distance(zombieScreen, startScreen);
                float sidePenalty = Mathf.Abs(horizontal) * 0.35f;
                float score = distanceFromCannon + sidePenalty;
                if (score < bestScore)
                {
                    best = zombie;
                    bestScore = score;
                    hitPos = zombieScreen;
                }
            }

            return best;
        }

        private bool IsScreenPointVisible(Vector2 screenPoint)
        {
            return screenPoint.x >= 0f
                && screenPoint.x <= Screen.width
                && screenPoint.y >= 0f
                && screenPoint.y <= Screen.height;
        }

        private void SpawnZombie(ZombieType type)
        {
            var zombie = GetAvailableZombie();
            if (zombie == null)
            {
                Debug.LogWarning("[CannonGame] ZombiePool exhausted. Increase MCP ZombiePool size.");
                return;
            }

            zombie.transform.SetParent(_spawnArea, false);
            float width = Mathf.Max(1f, _spawnArea.rect.width);
            float height = Mathf.Max(1f, _spawnArea.rect.height);
            float size = GetApproxSize(type);
            float horizontalMargin = size * 0.68f + 10f;
            float topMargin = size * 0.78f + 16f;
            float laneWidth = width / 5f;
            int lane = Random.Range(0, 5);
            float x = laneWidth * (lane + 0.5f) + Random.Range(-laneWidth * 0.22f, laneWidth * 0.22f);
            x = Mathf.Clamp(x, horizontalMargin, width - horizontalMargin);
            float y = Mathf.Clamp(height - Random.Range(topMargin, topMargin + 92f), size * 0.72f, height - topMargin);

            zombie.Initialize(new Vector2(x, y), type, GetSpeedMultiplier());
            zombie.OnReachedBottom = OnZombieReachedBottom;
            zombie.gameObject.SetActive(true);
            _activeZombies.Add(zombie);
        }

        private void BindPool()
        {
            _zombiePool.Clear();
            var root = FindChild(transform, "ZombiePool");
            if (root == null)
            {
                Debug.LogError("[CannonGame] Missing MCP ZombiePool.");
                return;
            }

            foreach (var zombie in root.GetComponentsInChildren<Zombie>(true))
            {
                zombie.gameObject.SetActive(false);
                _zombiePool.Add(zombie);
            }
        }

        private Zombie GetAvailableZombie()
        {
            foreach (var zombie in _zombiePool)
            {
                if (zombie != null && !zombie.gameObject.activeInHierarchy)
                    return zombie;
            }
            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private ZombieType GetZombieType()
        {
            float r = Random.value;
            if (_wave <= 1) return ZombieType.Normal;
            if (_wave == 2) return r < 0.34f ? ZombieType.Fast : ZombieType.Normal;
            if (_wave == 3)
            {
                if (r < 0.18f) return ZombieType.Tank;
                if (r < 0.50f) return ZombieType.Fast;
                return ZombieType.Normal;
            }
            if (_wave == 4)
            {
                if (r < 0.16f) return ZombieType.Bomber;
                if (r < 0.34f) return ZombieType.Tank;
                if (r < 0.58f) return ZombieType.Fast;
                return ZombieType.Normal;
            }

            if (r < 0.08f) return ZombieType.Boss;
            if (r < 0.25f) return ZombieType.Bomber;
            if (r < 0.43f) return ZombieType.Tank;
            if (r < 0.68f) return ZombieType.Fast;
            return ZombieType.Normal;
        }

        private float GetSpawnInterval()
        {
            return Mathf.Lerp(1.45f, 0.72f, Mathf.Clamp01((_wave - 1) / 5f));
        }

        private float GetSpeedMultiplier()
        {
            return 0.52f + Mathf.Min(0.24f, (_stage - 1) * 0.045f);
        }

        private static float GetApproxSize(ZombieType type)
        {
            switch (type)
            {
                case ZombieType.Fast: return 66f;
                case ZombieType.Tank: return 112f;
                case ZombieType.Bomber: return 88f;
                case ZombieType.Boss: return 138f;
                default: return 78f;
            }
        }

        private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b, out float progress)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq <= 0.0001f)
            {
                progress = 0f;
                return Vector2.Distance(point, a);
            }

            progress = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lenSq);
            Vector2 projection = a + ab * progress;
            return Vector2.Distance(point, projection);
        }
    }
}
