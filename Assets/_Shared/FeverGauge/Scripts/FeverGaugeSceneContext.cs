using UnityEngine;

namespace FeverGauge.Sandbox
{
    public sealed class FeverGaugeSceneContext : MonoBehaviour
    {
        [SerializeField] private FeverGaugeMgr feverGaugeMgr;
        //[SerializeField] private FeverGaugeUI feverGaugeUI;

        public FeverGaugeMgr FeverGaugeMgr => feverGaugeMgr;
        //public FeverGaugeUI FeverGaugeUI => feverGaugeUI;
    }
}
