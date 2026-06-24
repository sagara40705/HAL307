using Context;
using Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace TaiyouVoiceAI
{
    public class VoiceAiContext : ContextBase
    {
        [SerializeField, FormerlySerializedAs("_playerConextName")] private string _playerContextName;
        [SerializeField] private PlayerContext _playerContext;

        private void Start()
        {
            ResolveExternalContexts();
        }

        public PlayerContext GetPlayerContext()
        {
            ResolveExternalContexts();
            return _playerContext;
        }

        private void ResolveExternalContexts()
        {
            if (OnlyMyScene || _playerContext != null)
            {
                return;
            }

            _playerContext = FindContext<PlayerContext>(_playerContextName);
        }
    }
}
