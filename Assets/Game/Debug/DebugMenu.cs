#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.DebugTools
{
    public sealed class DebugMenu : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private Rect _windowRect = new Rect(20f, 20f, 320f, 260f);
        [SerializeField] private string _windowTitle = "Debug Menu";

        [Header("Sample Values")]
        [SerializeField] private int _goldAddAmount = 1000;
        [SerializeField] private Vector3 _resetPosition = Vector3.zero;

        private bool _isOpen;
        private bool _isInvincible;
        private int _gold;
        private int _windowId;

        private void Awake()
        {
            _windowId = GetInstanceID();
        }

        private void Update()
        {
            if (DebugShortcuts.Combo(Key.D, Key.Space, Key.LeftCtrl))
            {
                _isOpen = !_isOpen;
            }
        }

        private void OnGUI()
        {
            if (!_isOpen)
            {
                return;
            }

            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, _windowTitle);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Scene : {SceneManager.GetActiveScene().name}");
            GUILayout.Label($"Invincible : {(_isInvincible ? "ON" : "OFF")}");
            GUILayout.Label($"Gold : {_gold}");
            GUILayout.Space(8f);

            if (GUILayout.Button("HP Full"))
            {
                ExecuteHpFull();
            }

            if (GUILayout.Button($"Add Gold +{_goldAddAmount}"))
            {
                ExecuteAddGold(_goldAddAmount);
            }

            if (GUILayout.Button($"Toggle Invincible ({(_isInvincible ? "ON" : "OFF")})"))
            {
                ExecuteToggleInvincible();
            }

            if (GUILayout.Button("Reset Player Position"))
            {
                ExecuteResetPlayerPosition();
            }

            if (GUILayout.Button("Log Current Scene"))
            {
                ExecuteLogCurrentScene();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Close"))
            {
                _isOpen = false;
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private void ExecuteHpFull()
        {
            Debug.Log("[DebugMenu] HP Full");
            // ここで実際の HP 回復処理を呼ぶ
            // 例: PlayerHealth.Instance.FullHeal();
        }

        private void ExecuteAddGold(int amount)
        {
            _gold += amount;
            Debug.Log($"[DebugMenu] Add Gold: +{amount} / Total: {_gold}");
            // ここで実際の通貨管理クラスへ反映してもよい
            // 例: PlayerWallet.Instance.Add(amount);
        }

        private void ExecuteToggleInvincible()
        {
            _isInvincible = !_isInvincible;
            Debug.Log($"[DebugMenu] Invincible: {_isInvincible}");
            // ここで実際のプレイヤー無敵フラグへ反映
            // 例: PlayerStatus.Instance.SetInvincible(_isInvincible);
        }

        private void ExecuteResetPlayerPosition()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[DebugMenu] Player tag object was not found.");
                return;
            }

            player.transform.position = _resetPosition;
            Debug.Log($"[DebugMenu] Reset Player Position: {_resetPosition}");
        }

        private void ExecuteLogCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[DebugMenu] Current Scene: {sceneName}");
        }
    }
}
#endif
