using Context;
using Pose;
using UnityEngine;

namespace Player
{
    public class PlayerContext : ContextBase
    {
        [SerializeField] private PlayerPoseContoroller _poseController;
        public PlayerPoseContoroller GetPlayerPoseContoroller => _poseController;

        [SerializeField] private string _poseContextName;
        [SerializeField] private PoseContext _poseContext;

        private void Start()
        {
            ResolveExternalContexts();
        }

        public PoseContext GetPoseContext()
        {
            ResolveExternalContexts();
            return _poseContext;
        }

        private void ResolveExternalContexts()
        {
            if (OnlyMyScene || _poseContext != null)
            {
                return;
            }

            _poseContext = FindContext<PoseContext>(_poseContextName);
        }
    }
}
