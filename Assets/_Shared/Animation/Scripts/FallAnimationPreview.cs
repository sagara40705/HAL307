using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Animation
{
    /// <summary>
    /// 「Fall」クリップを単独で確認するためのプレビュー環境を構築します。
    /// </summary>
    public sealed class FallAnimationPreview : MonoBehaviour
    {
        [SerializeField] private GameObject _characterPrefab;
        [SerializeField] private AnimationClip _fallClip;

        private PlayableGraph _graph;

        private void Start()
        {
            CreatePreviewEnvironment();
            PlayFall();
        }

        private void Update()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                PlayFall();
            }
        }

        private void OnDestroy()
        {
            DestroyGraph();
        }

        private void PlayFall()
        {
            if (_characterPrefab == null || _fallClip == null)
            {
                Debug.LogError("[FallAnimationPreview] Character prefab or Fall clip is not assigned.", this);
                return;
            }

            GameObject character = GameObject.Find("Fall Preview Character");
            if (character == null)
            {
                character = Instantiate(_characterPrefab, Vector3.zero, Quaternion.identity);
                character.name = "Fall Preview Character";
            }

            Animator animator = character.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError("[FallAnimationPreview] Animator was not found.", character);
                return;
            }

            DestroyGraph();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create("Fall Animation Preview");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Fall", animator);
            AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, _fallClip);
            playable.SetApplyFootIK(true);
            output.SetSourcePlayable(playable);
            _graph.Play();
        }

        private void DestroyGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private static void CreatePreviewEnvironment()
        {
            if (Camera.main == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 1.7f, 5.5f);
                camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            }

            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            if (GameObject.Find("Preview Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Preview Ground";
                ground.transform.localScale = new Vector3(2f, 1f, 2f);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(16f, 16f, 420f, 24f), "Fall Animation Preview - Space: Replay");
        }
    }
}
