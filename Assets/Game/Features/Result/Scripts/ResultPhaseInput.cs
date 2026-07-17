using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ResultPhaseInput : MonoBehaviour
{
    private bool _isTransitionRequested;

    private void OnEnable()
    {
        _isTransitionRequested = false;
    }

    private void Update()
    {
        if (_isTransitionRequested || !WasEnterPressed())
        {
            return;
        }

        GamePhaseManager phaseManager = GamePhaseManager.Instance;
        if (phaseManager == null || !phaseManager.IsInitialized || phaseManager.IsChangingPhase)
        {
            return;
        }

        if (phaseManager.ChangePhase(GamePhase.Title))
        {
            _isTransitionRequested = true;
        }
    }

    private static bool WasEnterPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
               (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
    }
}
