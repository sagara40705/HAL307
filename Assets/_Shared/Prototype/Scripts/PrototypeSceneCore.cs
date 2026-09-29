using System.Collections.Generic;
using UnityEngine;

namespace Prototype.Sandbox
{
    // プロトタイプsceneで統合するscene名をリストで管理
    // scene開始時にすべてadditiveで読み込む
    public class ProttypeSceneCore : SceneCoreBase
    {
        // 統合するsceneのリスト
        [SerializeField] private List<string> _scenes;

        // scene開始時にすべてadditiveで読み込む
        protected override void Start()
        {
            StartCoroutine(LoadAllScenesCoroutine(SceneLoader.Instance, _scenes));
        }
    }
}
