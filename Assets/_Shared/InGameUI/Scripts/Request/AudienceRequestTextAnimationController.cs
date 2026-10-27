using System;
using System.Collections.Generic;
using UnityEngine;
using Pose;
using TMPro;

namespace AudienceRequest
{
    public class AudienceRequestTextAnimationController : MonoBehaviour
    {
        [Serializable]
        private class PoseTextAnimation
        {
            // PoseIDとAnimatorの組み合わせを保持するクラス
            [SerializeField] private PoseID _poseID;
            [SerializeField] private Animator _animator;

            // プロパティで外部からアクセスできるようにする
            public PoseID PoseID => _poseID;
            public Animator Animator => _animator;

            [NonSerialized] public TMP_Text AnimatedText;
            [NonSerialized] public TMP_Text Label;
            [NonSerialized] public Color LabelColor;
            [NonSerialized] public float LabelSize;
            [NonSerialized] public bool Initialized;
            [NonSerialized] public bool Playing;
            [NonSerialized] public bool Started;
        }

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int FeedbackState = Animator.StringToHash("TextAnimation");

        [Header("PoseごとのテキストAnimator")]
        [SerializeField]
        private List<PoseTextAnimation> _poseTextAnimations = new();


        [Header("Animator Trigger")]
        [SerializeField]
        private string _voiceClearTriggerName = "VoiceClear";

        private void Awake()
        {
            foreach (PoseTextAnimation setting in _poseTextAnimations) { Initialize(setting); }
        }

        private void OnEnable()
        {
            foreach (PoseTextAnimation setting in _poseTextAnimations)
            {
                Initialize(setting);
                RestoreLabel(setting);
                Animator animator = setting.Animator;
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.ResetTrigger(_voiceClearTriggerName);
                    animator.Play(IdleState, 0, 0f);
                }
            }
        }

        private static void Initialize(PoseTextAnimation setting)
        {
            if (setting.Initialized || setting.Animator == null) { return; }
            setting.AnimatedText = setting.Animator.GetComponent<TMP_Text>();
            setting.Label = setting.Animator.transform.parent != null
                ? setting.Animator.transform.parent.GetComponent<TMP_Text>() : null;
            if (setting.AnimatedText == null || setting.Label == null) { return; }
            setting.LabelColor = setting.Label.color;
            setting.LabelSize = setting.Label.fontSize;
            setting.Initialized = true;
            // The old child is offset and enlarged over the permanent legend. Keep it as
            // the existing animation's timing/color source, but render only one label.
            setting.AnimatedText.enabled = false;
        }

        private static void RestoreLabel(PoseTextAnimation setting)
        {
            setting.Playing = false;
            setting.Started = false;
            if (!setting.Initialized) { return; }
            if (setting.AnimatedText != null) { setting.AnimatedText.enabled = false; }
            if (setting.Label == null) { return; }
            setting.Label.color = setting.LabelColor;
            setting.Label.fontSize = setting.LabelSize;
        }

        private void LateUpdate()
        {
            foreach (PoseTextAnimation setting in _poseTextAnimations)
            {
                if (!setting.Initialized || !setting.Playing) { continue; }
                Animator animator = setting.Animator;
                if (animator == null || setting.Label == null || setting.AnimatedText == null)
                {
                    RestoreLabel(setting);
                    continue;
                }

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash != FeedbackState)
                {
                    if (setting.Started) { RestoreLabel(setting); }
                    continue;
                }
                setting.Started = true;
                if (state.normalizedTime >= 1f) { RestoreLabel(setting); continue; }

                Color animatedColor = setting.AnimatedText.color;
                float strength = Mathf.Clamp01(animatedColor.a);
                animatedColor.a = setting.LabelColor.a;
                setting.Label.color = Color.Lerp(setting.LabelColor, animatedColor, strength);
                float expansion = setting.LabelSize > 0f
                    ? Mathf.Max(0f, setting.AnimatedText.fontSize / setting.LabelSize - 1f) : 0f;
                // Retain the existing fade/pulse without growing into the adjacent legend rows.
                setting.Label.fontSize = setting.LabelSize * (1f + Mathf.Min(0.12f, expansion * strength));
            }
        }

        private void OnDisable()
        {
            foreach (PoseTextAnimation setting in _poseTextAnimations) { RestoreLabel(setting); }
        }

        /// <summary>
        /// 音声入力によってリクエストを消したときに呼ぶ
        /// </summary>
        public void PlayVoiceClearAnimation(PoseID poseID)
        {
            // PoseIDに対応するAnimatorを探す
            PoseTextAnimation setting =
                _poseTextAnimations.Find(x => x.PoseID == poseID);


            // 設定が見つからない場合は警告を出す
            if (setting == null)
            {
                Debug.LogWarning(
                    $"[AudienceRequestTextAnimationController] " +
                    $"Text animation setting was not found. Pose={poseID}"
                );

                return;
            }

            Animator animator = setting.Animator;

            // Animatorが未設定の場合は警告を出す
            if (animator == null)
            {
                Debug.LogWarning(
                    $"[AudienceRequestTextAnimationController] " +
                    $"Animator is not assigned. Pose={poseID}"
                );

                return;
            }

            // Animatorが非アクティブの場合はTriggerを設定しても反応しないので警告を出す
            if (!animator.gameObject.activeInHierarchy)
            {
                Debug.LogWarning(
                    $"[AudienceRequestTextAnimationController] " +
                    $"Text object is inactive. Pose={poseID}"
                );

                return;
            }

            Initialize(setting);
            if (!setting.Initialized) { return; }
            setting.Playing = true;
            setting.Started = false;
            animator.ResetTrigger(_voiceClearTriggerName);
            // Restart the existing one-shot state instead of queuing a second trigger during a pulse.
            animator.Play(FeedbackState, 0, 0f);
        }
    }
}
