using UnityEngine;

namespace LoseWeight.Core
{
    public static class SceneNodeResolver
    {
        public static GameObject FindRequired(string path)
        {
            var go = FindByPath(path);
            if (go == null)
                Debug.LogError($"[SceneNodeResolver] Missing scene node: {path}");
            return go;
        }

        public static T FindRequiredComponent<T>(string path) where T : Component
        {
            var go = FindRequired(path);
            if (go == null) return null;

            var component = go.GetComponent<T>();
            if (component == null)
                Debug.LogError($"[SceneNodeResolver] Missing component {typeof(T).Name} on {path}");
            return component;
        }

        public static Transform FindChild(Transform root, string childName)
        {
            if (root == null) return null;
            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
        }

        private static GameObject FindByPath(string path)
        {
            var active = GameObject.Find(path);
            if (active != null) return active;

            string[] parts = path.Split('/');
            if (parts.Length == 0) return null;

            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var candidate in all)
            {
                if (candidate == null || candidate.name != parts[0]) continue;
                if (!candidate.scene.IsValid()) continue;

                Transform current = candidate.transform;
                bool matched = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    current = FindDirectChild(current, parts[i]);
                    if (current == null)
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return current.gameObject;
            }

            return null;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == childName) return child;
            }
            return null;
        }
    }
}
