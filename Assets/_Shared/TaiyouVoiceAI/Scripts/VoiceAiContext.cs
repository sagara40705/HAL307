using UnityEngine;
using Player;
using Context;

namespace TaiyouVoiceAI
{
    public class VoiceAiContext : ContextBase
    {
        // Player
        [SerializeField] private string _playerSceneName;
        [SerializeField] private string _playerConextName;
        [SerializeField] private PlayerContext _playerContext;
        public PlayerContext GetPlayerContext()
        {
            if (OnlyMyScene) { return null; }
            _playerContext = FindContext<PlayerContext>(_playerSceneName, _playerConextName);
            return _playerContext;
        }
    }
}
