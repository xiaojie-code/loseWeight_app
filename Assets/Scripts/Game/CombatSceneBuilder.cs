using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.Game
{
    public class CombatSceneBuilder : MonoBehaviour
    {
        private GameObject _combatRoot;
        private Transform _hudRoot;
        private Text _playerHpText;
        private Text _opponentHpText;
        private Text _timerText;
        private Text _actionFeedback;
        private Text _poseBroadcastText;
        private Text _statusText;
        private RawImage _cameraPreviewImage;
        private Text _cameraPreviewLabel;
        private Texture _externalCameraPreview;

        public void ActivateScene()
        {
            BindScene();
            if (_combatRoot != null) _combatRoot.SetActive(true);
        }

        public void DestroyScene()
        {
            if (_combatRoot != null) _combatRoot.SetActive(false);
        }

        public void SetExternalCameraPreview(Texture texture)
        {
            _externalCameraPreview = texture;
            UpdateCameraPreview();
        }

        public void UpdateHUD(int playerHp, int opponentHp, int round, float timer)
        {
            EnsureHUD();
            if (_playerHpText != null) _playerHpText.text = $"我方 HP: {playerHp}";
            if (_opponentHpText != null) _opponentHpText.text = $"对手 HP: {opponentHp}";
            if (_timerText != null) _timerText.text = $"R{round} | {Mathf.CeilToInt(timer)}s";
        }

        public void ShowStatus(string text)
        {
            EnsureHUD();
            if (_statusText != null) _statusText.text = text;
        }

        public void ShowActionFeedback(string text)
        {
            EnsureHUD();
            if (_actionFeedback == null) return;
            _actionFeedback.text = text;
            CancelInvoke(nameof(ClearFeedback));
            Invoke(nameof(ClearFeedback), 0.8f);
        }

        public void ShowPoseBroadcast(string actionName, string detail)
        {
            EnsureHUD();
            if (_poseBroadcastText == null) return;
            _poseBroadcastText.text = string.IsNullOrEmpty(detail)
                ? $"ACTION: {actionName}"
                : $"ACTION: {actionName}\n{detail}";
            CancelInvoke(nameof(ClearPoseBroadcast));
            Invoke(nameof(ClearPoseBroadcast), 1.8f);
        }

        public void OpponentHit()
        {
            var opponent = FindChild(_combatRoot != null ? _combatRoot.transform : null, "Opponent");
            if (opponent != null) StartCoroutine(HitReaction(opponent));
        }

        public void TriggerOpponentAnimation(string trigger)
        {
        }

        private void BindScene()
        {
            if (_combatRoot != null) return;
            _combatRoot = SceneNodeResolver.FindRequired("AppSceneRoot/CombatSceneRoot");
            EnsureHUD();
        }

        private void EnsureHUD()
        {
            if (_hudRoot == null)
            {
                if (_combatRoot == null)
                    _combatRoot = SceneNodeResolver.FindRequired("AppSceneRoot/CombatSceneRoot");
                _hudRoot = FindChild(_combatRoot != null ? _combatRoot.transform : null, "CombatHUD");
            }
            if (_hudRoot == null)
            {
                Debug.LogError("[CombatScene] Missing MCP CombatHUD.");
                return;
            }

            _playerHpText = FindText("PlayerHP");
            _opponentHpText = FindText("OpponentHP");
            _timerText = FindText("Timer");
            _statusText = FindText("Status");
            _poseBroadcastText = FindText("PoseBroadcast");
            _actionFeedback = FindText("Action");
            _cameraPreviewImage = FindChild(_hudRoot, "CameraImage")?.GetComponent<RawImage>();
            _cameraPreviewLabel = FindText("CameraPreviewLabel");
            UpdateCameraPreview();
        }

        private void UpdateCameraPreview()
        {
            if (_cameraPreviewImage == null) return;
            if (_externalCameraPreview != null)
            {
                _cameraPreviewImage.enabled = true;
                _cameraPreviewImage.texture = _externalCameraPreview;
                if (_cameraPreviewLabel != null) _cameraPreviewLabel.text = "ML KIT CAMERA";
            }
            else
            {
                _cameraPreviewImage.enabled = false;
                if (_cameraPreviewLabel != null) _cameraPreviewLabel.text = "CAMERA WAITING";
            }
        }

        private Text FindText(string name)
        {
            var child = FindChild(_hudRoot, name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private void ClearFeedback()
        {
            if (_actionFeedback != null) _actionFeedback.text = "";
        }

        private void ClearPoseBroadcast()
        {
            if (_poseBroadcastText != null) _poseBroadcastText.text = "";
        }

        private static IEnumerator HitReaction(Transform target)
        {
            var original = target.localPosition;
            target.localPosition = original + new Vector3(0, 0, 0.15f);
            yield return new WaitForSeconds(0.1f);
            if (target != null) target.localPosition = original;
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
