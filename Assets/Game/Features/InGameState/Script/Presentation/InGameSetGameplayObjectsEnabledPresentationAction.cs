using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSetGameplayObjectsEnabledPresentationAction", menuName = "Game/InGame/Presentation/Set Gameplay Objects Enabled")]
public sealed class InGameSetGameplayObjectsEnabledPresentationAction : InGamePresentationAction
{
    [SerializeField] private bool targetEnabled = true;
    [SerializeField] private bool includeMainCamera = true;
    [SerializeField] private bool includeEnemies = true;
    [SerializeField] private bool includePlayer = true;
    [SerializeField] private bool includeBehaviours = true;
    [SerializeField] private bool includeRenderers = true;
    [SerializeField] private bool includeColliders = true;

    private static readonly List<Behaviour> MainCameraBehaviours = new();
    private static readonly List<Behaviour> ActorBehaviours = new();
    private static readonly List<Renderer> ActorRenderers = new();
    private static readonly List<Collider> ActorColliders = new();
    private static readonly Dictionary<Behaviour, bool> BehaviourStates = new();
    private static readonly Dictionary<Renderer, bool> RendererStates = new();
    private static readonly Dictionary<Collider, bool> ColliderStates = new();

    public override IEnumerator Play(InGameFlowContext context)
    {
        if (targetEnabled)
        {
            EnableTargets(context);
        }
        else
        {
            DisableTargets(context);
        }

        yield break;
    }

    private void DisableTargets(InGameFlowContext context)
    {
        if (includeMainCamera)
        {
            CaptureMainCamera(context);
            SetBehavioursEnabled(MainCameraBehaviours, false);
        }

        CaptureActors(context);
        SetActorsEnabled(false);
    }

    private void EnableTargets(InGameFlowContext context)
    {
        if (includeMainCamera)
        {
            if (MainCameraBehaviours.Count <= 0)
            {
                CaptureMainCamera(context);
            }

            SetBehavioursEnabled(MainCameraBehaviours, true);
            MainCameraBehaviours.Clear();
        }

        if (ActorBehaviours.Count <= 0 && ActorRenderers.Count <= 0 && ActorColliders.Count <= 0)
        {
            CaptureActors(context);
        }

        SetActorsEnabled(true);
        ActorBehaviours.Clear();
        ActorRenderers.Clear();
        ActorColliders.Clear();
    }

    private void CaptureMainCamera(InGameFlowContext context)
    {
        Camera camera = FindMainCamera(context);
        if (camera == null)
        {
            Debug.LogWarning("[InGameSetGameplayObjectsEnabledPresentationAction] Main camera was not found.");
            return;
        }

        AddBehaviours(camera.gameObject, MainCameraBehaviours);
    }

    private Camera FindMainCamera(InGameFlowContext context)
    {
        Transform cameraTransform = context != null && context.ForwardProgressMover != null
            ? context.ForwardProgressMover.CameraTransform
            : null;

        if (cameraTransform != null)
        {
            Camera contextCamera = cameraTransform.GetComponent<Camera>();
            if (contextCamera != null)
            {
                return contextCamera;
            }

            contextCamera = cameraTransform.GetComponentInChildren<Camera>(true);
            if (contextCamera != null)
            {
                return contextCamera;
            }

            contextCamera = cameraTransform.GetComponentInParent<Camera>();
            if (contextCamera != null)
            {
                return contextCamera;
            }
        }

        return Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
    }

    private void CaptureActors(InGameFlowContext context)
    {
        HashSet<GameObject> actorRoots = new();

        if (includeEnemies)
        {
            AddEnemies(context, actorRoots);
        }

        if (includePlayer)
        {
            AddPlayer(actorRoots);
        }

        foreach (GameObject actorRoot in actorRoots)
        {
            if (actorRoot == null)
            {
                continue;
            }

            if (includeBehaviours)
            {
                AddBehaviours(actorRoot, ActorBehaviours);
            }

            if (includeRenderers)
            {
                AddRenderers(actorRoot, ActorRenderers);
            }

            if (includeColliders)
            {
                AddColliders(actorRoot, ActorColliders);
            }
        }
    }

    private void AddEnemies(InGameFlowContext context, HashSet<GameObject> actorRoots)
    {
        EnemyLineupManager lineup = context != null ? context.EnemyLineup : null;
        AddEnemy(lineup != null ? lineup.Enemy1 : null, actorRoots);
        AddEnemy(lineup != null ? lineup.Enemy2 : null, actorRoots);
        AddEnemy(lineup != null ? lineup.PendingOldEnemy : null, actorRoots);
        AddEnemy(lineup != null ? lineup.PendingNewEnemy : null, actorRoots);

        EnemyRegistry registry = Object.FindFirstObjectByType<EnemyRegistry>();
        if (registry != null)
        {
            foreach (global::Enemy.Enemy enemy in registry.Enemies)
            {
                AddEnemy(enemy, actorRoots);
            }
        }

        global::Enemy.Enemy[] enemies = Object.FindObjectsByType<global::Enemy.Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (global::Enemy.Enemy enemy in enemies)
        {
            AddEnemy(enemy, actorRoots);
        }
    }

    private void AddEnemy(global::Enemy.Enemy enemy, HashSet<GameObject> actorRoots)
    {
        if (enemy == null)
        {
            return;
        }

        actorRoots.Add(enemy.gameObject);
    }

    private void AddPlayer(HashSet<GameObject> actorRoots)
    {
        Player.PlayerPoseContoroller player = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogWarning("[InGameSetGameplayObjectsEnabledPresentationAction] Player was not found.");
            return;
        }

        actorRoots.Add(player.gameObject);
    }

    private void SetActorsEnabled(bool enabled)
    {
        if (includeBehaviours)
        {
            SetBehavioursEnabled(ActorBehaviours, enabled);
        }

        if (includeRenderers)
        {
            SetRenderersEnabled(ActorRenderers, enabled);
        }

        if (includeColliders)
        {
            SetCollidersEnabled(ActorColliders, enabled);
        }
    }

    private void AddBehaviours(GameObject root, List<Behaviour> behaviours)
    {
        if (root == null)
        {
            return;
        }

        Behaviour[] components = root.GetComponentsInChildren<Behaviour>(true);
        foreach (Behaviour component in components)
        {
            if (component == null || behaviours.Contains(component))
            {
                continue;
            }

            behaviours.Add(component);
        }
    }

    private void AddRenderers(GameObject root, List<Renderer> renderers)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] components = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer component in components)
        {
            if (component == null || renderers.Contains(component))
            {
                continue;
            }

            renderers.Add(component);
        }
    }

    private void AddColliders(GameObject root, List<Collider> colliders)
    {
        if (root == null)
        {
            return;
        }

        Collider[] components = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider component in components)
        {
            if (component == null || colliders.Contains(component))
            {
                continue;
            }

            colliders.Add(component);
        }
    }

    private void SetBehavioursEnabled(List<Behaviour> behaviours, bool enabled)
    {
        for (int i = behaviours.Count - 1; i >= 0; i--)
        {
            Behaviour component = behaviours[i];
            if (component == null)
            {
                behaviours.RemoveAt(i);
                continue;
            }

            if (enabled)
            {
                component.enabled = BehaviourStates.TryGetValue(component, out bool previousState) ? previousState : true;
                BehaviourStates.Remove(component);
            }
            else
            {
                if (!BehaviourStates.ContainsKey(component))
                {
                    BehaviourStates.Add(component, component.enabled);
                }

                component.enabled = false;
            }
        }
    }

    private void SetRenderersEnabled(List<Renderer> renderers, bool enabled)
    {
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            Renderer component = renderers[i];
            if (component == null)
            {
                renderers.RemoveAt(i);
                continue;
            }

            if (enabled)
            {
                component.enabled = RendererStates.TryGetValue(component, out bool previousState) ? previousState : true;
                RendererStates.Remove(component);
            }
            else
            {
                if (!RendererStates.ContainsKey(component))
                {
                    RendererStates.Add(component, component.enabled);
                }

                component.enabled = false;
            }
        }
    }

    private void SetCollidersEnabled(List<Collider> colliders, bool enabled)
    {
        for (int i = colliders.Count - 1; i >= 0; i--)
        {
            Collider component = colliders[i];
            if (component == null)
            {
                colliders.RemoveAt(i);
                continue;
            }

            if (enabled)
            {
                component.enabled = ColliderStates.TryGetValue(component, out bool previousState) ? previousState : true;
                ColliderStates.Remove(component);
            }
            else
            {
                if (!ColliderStates.ContainsKey(component))
                {
                    ColliderStates.Add(component, component.enabled);
                }

                component.enabled = false;
            }
        }
    }
}
