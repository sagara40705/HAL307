using UnityEngine;
using Pose;
using UnityEngine.SceneManagement;

namespace Player
{
    public class PlayerContext : MonoBehaviour
    {
        //　外部
        [SerializeField] private string _poseParryJudgeScene;   // PoseParryJudgeシーンの名前
        [SerializeField] private PoseParryJudge _poseParryJudge;// PoseParryJudgeのプレハブ

        public PoseParryJudge PoseParryJudge => _poseParryJudge;
        private void Awake()
        {
            // PoseParryJudgeシーンが存在するか確認
            Scene scene = SceneManager.GetSceneByName(_poseParryJudgeScene);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"Scene '{_poseParryJudgeScene}' が未ロードまたは無効です。");
                return;
            }

            _poseParryJudge = FindInScene<PoseParryJudge>(scene);
            if (_poseParryJudge == null)
            {
                Debug.LogWarning($"Scene '{_poseParryJudgeScene}' で PoseParryJudge が見つかりません。");
                return;
            }

            Debug.Log($"取得成功: {_poseParryJudge.name}");

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
