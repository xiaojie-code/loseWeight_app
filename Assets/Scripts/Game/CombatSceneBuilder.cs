using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LoseWeight.Game
{
    public class CombatSceneBuilder : MonoBehaviour
    {
        private GameObject _arenaRoot;
        private GameObject _opponentModel;
        private GameObject _playerModel;
        private Text _playerHpText, _opponentHpText, _timerText, _actionFeedback, _statusText;

        public void BuildScene()
        {
            // \u5148\u6e05\u7406\u65e7\u573a\u666f\uff0c\u9632\u6b62\u91cd\u590d\u521b\u5efa
            DestroyScene();
            BuildArena();
            BuildCharacters();
            SetupCamera();
        }

        public void DestroyScene()
        {
            if (_arenaRoot) Destroy(_arenaRoot);
            _arenaRoot = null;
            _opponentModel = null;
            _playerModel = null;
        }

        private void BuildArena()
        {
            _arenaRoot = new GameObject("Arena");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_arenaRoot.transform);
            floor.transform.localPosition = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(8, 0.1f, 8);
            floor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.25f);
            Destroy(floor.GetComponent<Collider>());

            float r = 3.5f;
            CreateRope(new Vector3(0,0.8f,r), new Vector3(r*2,0.04f,0.04f));
            CreateRope(new Vector3(0,1.2f,r), new Vector3(r*2,0.04f,0.04f));
            CreateRope(new Vector3(0,0.8f,-r), new Vector3(r*2,0.04f,0.04f));
            CreateRope(new Vector3(0,1.2f,-r), new Vector3(r*2,0.04f,0.04f));
            CreateRope(new Vector3(-r,0.8f,0), new Vector3(0.04f,0.04f,r*2));
            CreateRope(new Vector3(-r,1.2f,0), new Vector3(0.04f,0.04f,r*2));
            CreateRope(new Vector3(r,0.8f,0), new Vector3(0.04f,0.04f,r*2));
            CreateRope(new Vector3(r,1.2f,0), new Vector3(0.04f,0.04f,r*2));

            var lg = new GameObject("Light");
            lg.transform.SetParent(_arenaRoot.transform);
            lg.transform.position = new Vector3(0, 5, -2);
            lg.transform.rotation = Quaternion.Euler(45, 0, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 0.8f;
        }

        private void BuildCharacters()
        {
            // \u8fd0\u884c\u65f6\u52a0\u8f7d\u8d44\u6e90\u914d\u7f6e
            var assets = Resources.Load<CombatAssets>("CombatAssets");
            GameObject prefab = null;
            RuntimeAnimatorController ctrl = null;

            if (assets != null)
            {
                prefab = assets.CharacterPrefab;
                ctrl = assets.CombatAnimator;
            }

            #if UNITY_EDITOR
            // Editor \u4e2d\u5982\u679c\u6ca1\u914d\u7f6e CombatAssets\uff0c\u7528 AssetDatabase \u5151\u5e95
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MeiShu/Frank_Fighting_Set4/Assets/Meshes/Frank_FS4_SkeletalMeshes.prefab");
                ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/FrankCombatAnimator.controller");
            }
            #endif

            if (prefab != null)
            {
                _opponentModel = Instantiate(prefab, _arenaRoot.transform);
                _opponentModel.name = "Opponent";
                _opponentModel.transform.localPosition = new Vector3(0, 0, 2f);
                _opponentModel.transform.localRotation = Quaternion.Euler(0, 180, 0);

                _playerModel = Instantiate(prefab, _arenaRoot.transform);
                _playerModel.name = "Player";
                _playerModel.transform.localPosition = new Vector3(0, 0, -1f);

                // \u786e\u4fdd\u4e24\u4e2a\u6a21\u578b\u7684 Animator Controller \u88ab\u6b63\u786e\u66ff\u6362
                SetupAnimator(_playerModel, ctrl);
                SetupAnimator(_opponentModel, ctrl);

                var existingDriver = gameObject.GetComponent<CharacterAnimationDriver>();
                if (existingDriver != null) Destroy(existingDriver);
                var animDriver = gameObject.AddComponent<CharacterAnimationDriver>();
                animDriver.Setup(_playerModel, _opponentModel);
                Debug.Log("[CombatScene] Models loaded");
            }
            else
            {
                Debug.LogWarning("[CombatScene] \u6a21\u578b\u672a\u627e\u5230\uff01\u8bf7\u5728 Resources/CombatAssets \u4e2d\u914d\u7f6e\u89d2\u8272Prefab");
            }
        }

        private void SetupAnimator(GameObject model, RuntimeAnimatorController ctrl)
        {
            if (model == null) return;

            // 禁用模型上所有已有的 Animator（防止自带 controller 自动播放）
            var allAnimators = model.GetComponentsInChildren<Animator>(true);
            foreach (var anim in allAnimators)
            {
                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }

            // 在根节点找或添加一个 Animator
            var mainAnim = model.GetComponent<Animator>();
            if (mainAnim == null)
            {
                mainAnim = model.AddComponent<Animator>();
            }
            mainAnim.enabled = true;
            mainAnim.applyRootMotion = false;

            // 复用子节点的 Avatar（骨骼绑定）
            foreach (var a in allAnimators)
            {
                if (a.avatar != null)
                {
                    mainAnim.avatar = a.avatar;
                    break;
                }
            }

            if (ctrl != null)
            {
                mainAnim.runtimeAnimatorController = ctrl;
                Debug.Log($"[CombatScene] {model.name} Animator OK, ctrl={ctrl.name}");
            }
            else
            {
                Debug.LogWarning($"[CombatScene] {model.name} no combat controller available");
            }
        }

        private void BuildHUD()
        {
            var hudGo = new GameObject("CombatHUD");
            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            hudGo.AddComponent<CanvasScaler>();
            hudGo.AddComponent<GraphicRaycaster>();

            // ���������HP
            _playerHpText = CreateText(hudGo.transform, "��� HP: 100",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(15, -15), new Vector2(180, 30), 18, Color.white, TextAnchor.UpperLeft);

            // �����ң�����HP
            _opponentHpText = CreateText(hudGo.transform, "���� HP: 100",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-15, -15), new Vector2(180, 30), 18, Color.white, TextAnchor.UpperRight);

            // �����У��غ�/ʱ��
            _timerText = CreateText(hudGo.transform, "Round 1 | 60s",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(200, 30), 18, Color.white, TextAnchor.UpperCenter);

            // �Ҳ��м䣺�������������ģ��̶�λ�ã�
            _actionFeedback = CreateText(hudGo.transform, "",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-15, 0), new Vector2(200, 40), 24, Color.yellow, TextAnchor.MiddleRight);

            // �м䣺״̬��ʾ
            _statusText = CreateText(hudGo.transform, "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(300, 40), 26, Color.white, TextAnchor.MiddleCenter);
        }

        private Text CreateText(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam) { cam.transform.position = new Vector3(0,2.5f,-4f); cam.transform.rotation = Quaternion.Euler(15,0,0); cam.fieldOfView = 50; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f,0.05f,0.1f); }
        }

        private void CreateRope(Vector3 p, Vector3 s)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.transform.SetParent(_arenaRoot.transform); r.transform.localPosition = p; r.transform.localScale = s;
            r.GetComponent<Renderer>().material.color = new Color(0.8f,0.1f,0.1f); Destroy(r.GetComponent<Collider>());
        }

        // === Public API ===
        public void UpdateHUD(int php, int ohp, int round, float timer)
        {
            if (_playerHpText) _playerHpText.text = $"��� HP: {php}";
            if (_opponentHpText) _opponentHpText.text = $"���� HP: {ohp}";
            if (_timerText) _timerText.text = $"R{round} | {Mathf.CeilToInt(timer)}s";
        }
        public void ShowStatus(string t) { if (_statusText) _statusText.text = t; }
        public void ShowActionFeedback(string t)
        {
            if (_actionFeedback) { _actionFeedback.text = t; CancelInvoke(nameof(ClearFB)); Invoke(nameof(ClearFB), 1f); }
        }
        private void ClearFB() { if (_actionFeedback) _actionFeedback.text = ""; }
        public void OpponentHit() { if (_opponentModel) StartCoroutine(HitAnim()); }
        private IEnumerator HitAnim() { var p = _opponentModel.transform.localPosition; _opponentModel.transform.localPosition = p + Vector3.forward*0.12f; yield return new WaitForSeconds(0.1f); _opponentModel.transform.localPosition = p; }
    }
}
