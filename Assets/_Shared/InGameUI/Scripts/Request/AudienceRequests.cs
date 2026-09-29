using System;
using System.Collections.Generic;
using UnityEngine;
using Pose;

namespace AudienceRequest
{
    [Serializable]
    public sealed class AudienceRequestEntry
    {
        public PoseID poseID;
        public int count;
    }

    [CreateAssetMenu(fileName = "AudienceRequest", menuName = "Game/AudienceRequests")]
    public class AudienceRequests : ScriptableObject
    {
        [SerializeField] private List<AudienceRequestEntry> _requests = new();

        public IReadOnlyList<AudienceRequestEntry> Requests => _requests;

        // リクエストの中からランダムにAudienceRequestを1つ取得する
        public AudienceRequestEntry GetRandomRequest()
        {
            if (_requests.Count == 0)
            {
                throw new InvalidOperationException("AudienceRequests is empty.");
            }

            int index = UnityEngine.Random.Range(0, _requests.Count);
            return _requests[index];
        }

        public int GetCount(PoseID poseID)
        {
            AudienceRequestEntry request = FindRequest(poseID);
            return request != null ? request.count : 0;
        }

        public void AddCount(PoseID poseID)
        {
            AudienceRequestEntry request = FindRequest(poseID);
            if (request != null)
            {
                request.count++;
            }
        }

        public void ClearCount(PoseID poseID)
        {
            AudienceRequestEntry request = FindRequest(poseID);
            if (request != null)
            {
                request.count = 0;
            }
        }

        public void ClearAll()
        {
            foreach (var request in _requests)
            {
                if (request != null)
                {
                    request.count = 0;
                }
            }
        }

        public bool Contains(PoseID poseID)
        {
            return FindRequest(poseID) != null;
        }

        private AudienceRequestEntry FindRequest(PoseID poseID)
        {
            foreach (AudienceRequestEntry request in _requests)
            {
                if (request != null && request.poseID == poseID)
                {
                    return request;
                }
            }

            return null;
        }
    }
}
