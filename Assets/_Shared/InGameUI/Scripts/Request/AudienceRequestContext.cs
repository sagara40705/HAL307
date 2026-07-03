using Context;
using Enemy;
using Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace AudienceRequest
{
    public class AudienceRequestContext : ContextBase
    {
        [SerializeField, FormerlySerializedAs("_playerConextName")] private string _playerContextName;
        [SerializeField] private PlayerContext _playerContext;

        [SerializeField, FormerlySerializedAs("_enemyConextName")] private string _enemyContextName;
        [SerializeField] private EnemyContext _enemyContext;

        [SerializeField] private FeverGaugeController _feverGaugeController;

        private void Start()
        {
            ResolveExternalContexts();
        }

        public PlayerContext GetPlayerContext()
        {
            ResolveExternalContexts();
            return _playerContext;
        }

        public EnemyContext GetEnemyContext()
        {
            ResolveExternalContexts();
            return _enemyContext;
        }

        public FeverGaugeController GetFeverGaugeController()
        {
            if (_feverGaugeController == null)
            {
                _feverGaugeController = FindFirstObjectByType<FeverGaugeController>();
            }

            return _feverGaugeController;
        }

        private void ResolveExternalContexts()
        {
            if (OnlyMyScene)
            {
                return;
            }

            if (_playerContext == null)
            {
                _playerContext = FindContext<PlayerContext>(_playerContextName);
            }

            if (_enemyContext == null)
            {
                _enemyContext = FindContext<EnemyContext>(_enemyContextName);
            }
        }
    }
}
