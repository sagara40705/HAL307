using UnityEngine;
using Player;
using UnityEngine.SceneManagement;

namespace AudienceRequest
{
    public class AudienceRequestContext : MonoBehaviour
    {
        [SerializeField] private string _playerScene;
        [SerializeField] private PlayerContext _playerContext;

        private void Awake()
        {
            // Playerシーンが存在するか確認
            Scene scene = SceneManager.GetSceneByName(_playerScene);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"Scene '{_playerScene}' が未ロードまたは無効です。");
                return;
            }

            _playerContext = FindInScene<PlayerContext>(scene);
            if (_playerContext == null)
            {
                Debug.LogWarning($"Scene '{_playerScene}' で PoseParryJudge が見つかりません。");
                return;
            }

            Debug.Log($"取得成功: {_playerContext.name}");
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true); // 非アクティブも探索
                if (found != null) return found;
            }
            return null;
        }
    }
}
