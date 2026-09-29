using System;
using System.Collections.Generic;
using UnityEngine;
using Pose;

namespace AudienceRequest
{
    [CreateAssetMenu(
        fileName = "AudienceRequestLevelDesignSettings",
        menuName = "Game/AudienceRequestLevelDesignSettings"
    )]
    public class AudienceRequestLevelDesignSettings : ScriptableObject
    {
        [Header("通常リクエスト生成")]
        [SerializeField] private float _normalRequestInterval = 5f;
        [SerializeField] private int _maxRequestCount = 3;
        [SerializeField] private int _spawnCountPerWave = 3;

        [Header("黒リクエスト生成")]
        [SerializeField] private float _blackRequestInterval = 15f;
        [SerializeField, Range(0f, 1f)] private float _blackRequestChance = 0.35f;

        [Header("通常リクエスト候補")]
        [SerializeField] private List<RequestSpawnWeight> _normalRequestWeights = new();

        [Header("黒リクエスト候補")]
        [SerializeField] private List<RequestSpawnWeight> _blackRequestWeights = new();

        public float NormalRequestInterval => _normalRequestInterval;
        public int MaxRequestCount => _maxRequestCount;
        public int SpawnCountPerWave => _spawnCountPerWave;

        public float BlackRequestInterval => _blackRequestInterval;
        public float BlackRequestChance => _blackRequestChance;

        public IReadOnlyList<RequestSpawnWeight> NormalRequestWeights => _normalRequestWeights;
        public IReadOnlyList<RequestSpawnWeight> BlackRequestWeights => _blackRequestWeights;

        public PoseID GetRandomNormalPose()
        {
            return GetWeightedRandomPose(_normalRequestWeights);
        }

        public PoseID GetRandomBlackPoseFromCandidates(List<PoseID> candidates)
        {
            return candidates == null ? PoseID.None : GetWeightedRandomPose(_blackRequestWeights, candidates);
        }

        public PoseID GetRandomNormalPoseFromCandidates(List<PoseID> candidates)
        {
            return candidates == null ? PoseID.None : GetWeightedRandomPose(_normalRequestWeights, candidates);
        }

        private static PoseID GetWeightedRandomPose(
            IReadOnlyList<RequestSpawnWeight> weights,
            List<PoseID> candidates = null)
        {
            if (weights == null || weights.Count == 0)
            {
                return PoseID.None;
            }

            int totalWeight = 0;

            foreach (RequestSpawnWeight weight in weights)
            {
                if (!IsEligible(weight, candidates))
                {
                    continue;
                }

                totalWeight += weight.Weight;
            }

            if (totalWeight <= 0)
            {
                return PoseID.None;
            }

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (RequestSpawnWeight weight in weights)
            {
                if (!IsEligible(weight, candidates))
                {
                    continue;
                }

                current += weight.Weight;

                if (randomValue < current)
                {
                    return weight.PoseID;
                }
            }

            return PoseID.None;
        }

        private static bool IsEligible(RequestSpawnWeight weight, List<PoseID> candidates)
        {
            return weight != null && weight.IsEnabled && weight.Weight > 0 &&
                   (candidates == null || candidates.Contains(weight.PoseID));
        }
    }

    [Serializable]
    public class RequestSpawnWeight
    {
        [SerializeField] private PoseID _poseID;
        [SerializeField] private int _weight = 1;
        [SerializeField] private bool _isEnabled = true;

        public PoseID PoseID => _poseID;
        public int Weight => _weight;
        public bool IsEnabled => _isEnabled;
    }
}
