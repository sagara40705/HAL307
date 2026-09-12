using System;
using System.Collections.Generic;
using UnityEngine;
using Pose;

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
        }

        [Header("PoseごとのテキストAnimator")]
        [SerializeField]
        private List<PoseTextAnimation> _poseTextAnimations = new();


        [Header("Animator Trigger")]
        [SerializeField]
        private string _voiceClearTriggerName = "VoiceClear";

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

            animator.ResetTrigger(_voiceClearTriggerName);
            animator.SetTrigger(_voiceClearTriggerName);
        }
    }
}
