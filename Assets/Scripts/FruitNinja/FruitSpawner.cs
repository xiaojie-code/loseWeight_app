using System.Collections.Generic;
using UnityEngine;

namespace LoseWeight.FruitNinja
{
    public class FruitSpawner : MonoBehaviour
    {
        public RectTransform SpawnArea;
        public float SpawnInterval = 1.2f;
        public float BombChance = 0.15f;
        public float Gravity = 600f;
        public System.Action OnFruitMissed;

        private readonly List<Fruit> _activeFruits = new List<Fruit>();
        private readonly List<Fruit> _fruitPool = new List<Fruit>();
        private float _nextSpawnTime;
        private float _elapsed;
        private bool _isSpawning;

        private static readonly Color[] FruitColors =
        {
            new Color(1f, 0.2f, 0.2f),
            new Color(1f, 0.6f, 0f),
            new Color(1f, 1f, 0.2f),
            new Color(0.2f, 0.9f, 0.2f),
            new Color(0.6f, 0.2f, 0.8f),
        };

        public void BindPool(Transform root)
        {
            _fruitPool.Clear();
            var pool = FindChild(root, "FruitPool");
            if (pool == null)
            {
                Debug.LogError("[FruitNinja] Missing MCP FruitPool.");
                return;
            }
            foreach (var fruit in pool.GetComponentsInChildren<Fruit>(true))
            {
                fruit.gameObject.SetActive(false);
                _fruitPool.Add(fruit);
            }
        }

        public void StartSpawning()
        {
            _elapsed = 0f;
            _nextSpawnTime = Time.time + 0.8f;
            _isSpawning = true;
            _activeFruits.Clear();
        }

        public void StopSpawning()
        {
            _isSpawning = false;
        }

        private void Update()
        {
            if (!_isSpawning || SpawnArea == null) return;
            _elapsed += Time.deltaTime;
            float currentInterval = Mathf.Max(0.45f, SpawnInterval - _elapsed * 0.008f);

            if (Time.time >= _nextSpawnTime)
            {
                SpawnFruit();
                _nextSpawnTime = Time.time + currentInterval;
                if (_elapsed > 30f && Random.value < 0.3f) SpawnFruit();
                if (_elapsed > 60f && Random.value < 0.2f) SpawnFruit();
            }

            _activeFruits.RemoveAll(f => f == null || !f.gameObject.activeInHierarchy);
        }

        private void SpawnFruit()
        {
            var fruit = GetAvailableFruit();
            if (fruit == null)
            {
                Debug.LogWarning("[FruitNinja] FruitPool exhausted. Increase MCP FruitPool size.");
                return;
            }

            fruit.transform.SetParent(SpawnArea, false);
            bool isBomb = Random.value < BombChance;
            Color color = isBomb ? new Color(0.15f, 0.15f, 0.15f) : FruitColors[Random.Range(0, FruitColors.Length)];
            float screenW = SpawnArea.rect.width;
            float startX = Random.Range(screenW * 0.1f, screenW * 0.9f);
            Vector2 startPos = new Vector2(startX, -50f);
            Vector2 velocity = new Vector2(Random.Range(-180f, 180f), Random.Range(850f, 1150f));

            fruit.gameObject.SetActive(true);
            fruit.Initialize(startPos, velocity, Gravity, color, isBomb);
            fruit.Score = isBomb ? -30 : 10;
            fruit.CollisionRadius = isBomb ? 55f : 65f;
            fruit.OnMissed = OnFruitMissed;
            _activeFruits.Add(fruit);
        }

        public List<Fruit> GetActiveFruits()
        {
            _activeFruits.RemoveAll(f => f == null || !f.gameObject.activeInHierarchy);
            return _activeFruits;
        }

        public void ClearAll()
        {
            foreach (var fruit in _activeFruits)
                if (fruit != null) fruit.Despawn();
            _activeFruits.Clear();
        }

        private Fruit GetAvailableFruit()
        {
            foreach (var fruit in _fruitPool)
            {
                if (fruit != null && !fruit.gameObject.activeInHierarchy)
                    return fruit;
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
    }
}
